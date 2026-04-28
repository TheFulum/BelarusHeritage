using BelarusHeritage.Services;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace BelarusHeritage.Middleware;

public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;

    public AuditLogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AuditLogService auditLogService)
    {
        var requestBodySnapshot = await CaptureRequestBodyAsync(context);
        var formSnapshot = await CaptureFormSnapshotAsync(context);
        await _next(context);

        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300)
            return;

        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
            return;

        // Avoid self-noise from viewing/filtering the log itself
        if (path.StartsWith("/AdminAuditLog", StringComparison.OrdinalIgnoreCase))
            return;

        if (path.StartsWith("/AdminDashboard", StringComparison.OrdinalIgnoreCase))
            return;

        var method = context.Request.Method;
        var isWriteMethod = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
        if (!isWriteMethod)
            return;

        var userIdRaw = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userId = int.TryParse(userIdRaw, out var parsedUserId) ? parsedUserId : (int?)null;
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        var routeController = context.GetRouteValue("controller")?.ToString();
        var routeAction = context.GetRouteValue("action")?.ToString();
        var controllerSegment = !string.IsNullOrWhiteSpace(routeController)
            ? routeController
            : path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "AdminUnknown";
        var entity = controllerSegment.StartsWith("Admin", StringComparison.OrdinalIgnoreCase)
            ? controllerSegment.Substring("Admin".Length)
            : controllerSegment;
        if (string.IsNullOrWhiteSpace(entity))
            entity = "unknown";

        var entityId = ResolveEntityId(context, requestBodySnapshot);
        var action = ResolveAction(method, routeAction, path, entityId);

        var payload = new
        {
            method,
            path,
            url = context.Request.GetDisplayUrl(),
            actionName = routeAction,
            query = context.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString()),
            request = requestBodySnapshot,
            form = formSnapshot
        };

        await auditLogService.LogAsync(
            userId,
            action,
            entity,
            entityId,
            newValues: payload,
            ipAddress: ipAddress);
    }

    private static string ResolveAction(string method, string? routeAction, string path, int? entityId)
    {
        var actionName = (routeAction ?? string.Empty).ToLowerInvariant();
        var p = path.ToLowerInvariant();

        if (HttpMethods.IsDelete(method) || actionName.Contains("delete") || p.Contains("/delete"))
            return "delete";
        if (actionName.Contains("publish") || p.Contains("publish"))
            return "publish";
        if (actionName.Contains("upload") || actionName.Contains("setmain") || actionName.Contains("mass") || actionName.Contains("save") || actionName.Contains("edit") || actionName.Contains("update") || actionName.Contains("set") || actionName.Contains("toggle"))
            return "update";
        if (actionName.Contains("create") || p.Contains("/create"))
            return "create";
        if (HttpMethods.IsPut(method) || HttpMethods.IsPatch(method))
            return "update";
        if (HttpMethods.IsPost(method))
            return entityId.HasValue && entityId.Value > 0 ? "update" : "create";
        return "unknown";
    }

    private static int? ResolveEntityId(HttpContext context, string? requestBodySnapshot)
    {
        if (int.TryParse(context.GetRouteValue("id")?.ToString(), out var idFromRoute))
            return idFromRoute;
        if (int.TryParse(context.Request.Query["id"], out var idFromQuery))
            return idFromQuery;

        if (context.Request.HasFormContentType)
        {
            try
            {
                var form = context.Request.Form;
                var formKeys = new[] { "id", "entityId", "objectId", "imageId", "routeId" };
                foreach (var key in formKeys)
                {
                    if (int.TryParse(form[key], out var formId))
                        return formId;
                }
            }
            catch
            {
                // ignore
            }
        }

        if (string.IsNullOrWhiteSpace(requestBodySnapshot))
            return null;

        try
        {
            if (requestBodySnapshot.Contains('=')) // x-www-form-urlencoded
            {
                var form = QueryHelpers.ParseQuery(requestBodySnapshot);
                var formKeys = new[] { "id", "entityId", "objectId", "imageId", "routeId" };
                foreach (var key in formKeys)
                {
                    if (form.TryGetValue(key, out var val) && int.TryParse(val.ToString(), out var formId))
                        return formId;
                }
            }

            using var doc = JsonDocument.Parse(requestBodySnapshot);
            if (doc.RootElement.ValueKind == JsonValueKind.Number && doc.RootElement.TryGetInt32(out var rootId))
                return rootId;

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var keys = new[] { "id", "entityId", "objectId", "imageId", "routeId" };
                foreach (var key in keys)
                {
                    if (doc.RootElement.TryGetProperty(key, out var prop))
                    {
                        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var parsed))
                            return parsed;
                        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out parsed))
                            return parsed;
                    }
                }
            }
        }
        catch
        {
            // Ignore body parsing issues
        }

        return null;
    }

    private static async Task<Dictionary<string, string>?> CaptureFormSnapshotAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
            return null;

        try
        {
            var form = await context.Request.ReadFormAsync();
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in form)
            {
                var value = pair.Value.ToString();
                if (value.Length > 300)
                    value = value[..300] + "... [truncated]";
                result[pair.Key] = value;
            }
            if (form.Files.Count > 0)
                result["_filesCount"] = form.Files.Count.ToString();
            return result.Count > 0 ? result : null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> CaptureRequestBodyAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            return null;

        if (context.Request.ContentLength is null or <= 0)
            return null;

        var contentType = context.Request.ContentType ?? string.Empty;
        if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return null;

        if (context.Request.ContentLength > 64 * 1024)
            return $"[body omitted: {context.Request.ContentLength} bytes]";

        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
            return null;

        if (body.Length > 4000)
            return body[..4000] + "... [truncated]";

        return body;
    }
}

public static class AuditLogMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLog(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLogMiddleware>();
    }
}
