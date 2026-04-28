using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Services;

namespace BelarusHeritage.Controllers.Admin;

[Authorize(Policy = "Admin")]
public class AdminAuditLogController : Controller
{
    private readonly AuditLogService _auditLogService;

    public AdminAuditLogController(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> Index(
        int? userId = null,
        string? logAction = null,
        string? entity = null,
        int? entityId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1)
    {
        const int pageSize = 50;
        page = Math.Max(1, page);

        // Fetch one extra row to detect whether there is a next page.
        var logs = await _auditLogService.GetLogsAsync(
            userId, logAction, entity, entityId, fromDate, toDate, page, pageSize + 1);
        var hasNextPage = logs.Count > pageSize;
        if (hasNextPage)
            logs = logs.Take(pageSize).ToList();

        ViewBag.UserId = userId;
        ViewBag.LogAction = logAction;
        ViewBag.Entity = entity;
        ViewBag.EntityId = entityId;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.CurrentPage = page;
        ViewBag.HasNextPage = hasNextPage;

        return View(logs);
    }
}
