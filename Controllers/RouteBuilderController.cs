using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Services;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Data;
using System.Globalization;
using System.Text.Json;
using HeritageRoute = BelarusHeritage.Models.Domain.Route;

namespace BelarusHeritage.Controllers;

public class RouteBuilderController : Controller
{
    private readonly RouteService _routeService;
    private readonly AppDbContext _context;

    public RouteBuilderController(RouteService routeService, AppDbContext context)
    {
        _routeService = routeService;
        _context = context;
    }

    public IActionResult Index(int? routeId, int? addObject)
    {
        ViewBag.RouteId = routeId.HasValue && routeId.Value > 0 ? routeId : null;
        ViewBag.AddObjectId = addObject.HasValue && addObject.Value > 0 ? addObject : null;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Public()
    {
        var routes = await _routeService.GetPublicRoutesAsync(100);
        return View(routes);
    }

    [HttpGet]
    public async Task<IActionResult> GetRouteDto(int routeId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var route = await _routeService.GetRouteAsync(routeId, userId);

        if (route == null)
            return NotFound();

        var dto = new
        {
            route.Id,
            route.Title,
            route.Description,
            route.IsPublic,
            route.TotalKm,
            route.StartAddress,
            StartLat = route.StartLat,
            StartLng = route.StartLng,
            route.EndAddress,
            EndLat = route.EndLat,
            EndLng = route.EndLng,
            route.SourceRouteId,
            route.SourceRouteTitle,
            route.SourceRouteShareToken,
            Stops = route.Stops
                .OrderBy(s => s.SortOrder)
                .Select(s => new
                {
                    s.ObjectId,
                    s.SortOrder,
                    s.Notes,
                    Object = s.Object == null ? null : new
                    {
                        s.Object.Id,
                        s.Object.Slug,
                        NameRu = s.Object.NameRu,
                        NameBe = s.Object.NameBe,
                        NameEn = s.Object.NameEn,
                        s.Object.MainImageUrl,
                        Lat = s.Object.Location != null ? (double?)s.Object.Location.Lat : null,
                        Lng = s.Object.Location != null ? (double?)s.Object.Location.Lng : null
                    }
                })
        };

        return Json(dto);
    }

    [HttpGet]
    public async Task<IActionResult> GetRoute(int routeId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var route = await _routeService.GetRouteAsync(routeId, userId);

        if (route == null)
            return NotFound();

        return Json(route);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyRoutesLite()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var routes = await _routeService.GetUserRoutesAsync(userId);

        var dto = routes.Select(r => new
        {
            r.Id,
            r.Title,
            StopsCount = r.Stops?.Count ?? 0,
            r.TotalKm,
            r.UpdatedAt,
            r.SourceRouteTitle,
            r.SourceRouteShareToken
        });

        return Json(dto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoute(string title, string? description)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var route = new HeritageRoute
        {
            UserId = userId,
            Title = title,
            Description = description,
            IsPublic = false
        };

        var created = await _routeService.CreateRouteAsync(route);
        return Json(new { success = true, routeId = created.Id });
    }

    [HttpPost]
    public async Task<IActionResult> AddStop(int routeId, int objectId, string? notes = null)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        await _routeService.AddStopAsync(routeId, objectId, notes);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveStop(int routeId, int objectId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        await _routeService.RemoveStopAsync(routeId, objectId);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> ReorderStops(int routeId, [FromBody] List<int> objectIds)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        await _routeService.ReorderStopsAsync(routeId, objectIds);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> OptimizeStops(int routeId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var optimized = await _routeService.OptimizeStopsAsync(routeId, userId);
        return Json(new { success = optimized });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRoute(
        int routeId,
        string title,
        string? description,
        bool isPublic,
        string? startAddress,
        string? startLat,
        string? startLng,
        string? endAddress,
        string? endLat,
        string? endLng)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var route = await _routeService.GetRouteAsync(routeId, userId);

        if (route == null)
            return NotFound();

        route.Title = title;
        route.Description = description;
        route.IsPublic = isPublic;
        route.StartAddress = isPublic ? null : string.IsNullOrWhiteSpace(startAddress) ? null : startAddress.Trim();
        route.StartLat = isPublic ? null : ParseCoord(startLat);
        route.StartLng = isPublic ? null : ParseCoord(startLng);
        route.EndAddress = isPublic ? null : string.IsNullOrWhiteSpace(endAddress) ? null : endAddress.Trim();
        route.EndLat = isPublic ? null : ParseCoord(endLat);
        route.EndLng = isPublic ? null : ParseCoord(endLng);

        await _routeService.UpdateRouteAsync(route);
        return Json(new { success = true });
    }

    private static decimal? ParseCoord(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (decimal.TryParse(raw.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return null;
    }

    [HttpPost]
    public async Task<IActionResult> DeleteRoute(int routeId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _routeService.DeleteRouteAsync(routeId, userId);

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> BuildRoadPolyline([FromBody] List<RoadPointDto>? points)
    {
        if (points == null || points.Count < 2 || points.Count > 24)
            return BadRequest(new { success = false, message = "invalid_points" });

        var coords = string.Join(";",
            points.Select(p => $"{p.Lng.ToString("F6", CultureInfo.InvariantCulture)},{p.Lat.ToString("F6", CultureInfo.InvariantCulture)}"));

        var url = $"https://router.project-osrm.org/route/v1/driving/{coords}?overview=full&geometries=geojson";

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("BelarusHeritageRouteBuilder/1.0");
            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, new { success = false, message = "provider_unavailable" });

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            if (!root.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                return Ok(new { success = false, message = "empty_route" });

            var geometry = routes[0].GetProperty("geometry");
            var coordinates = geometry.GetProperty("coordinates");
            if (coordinates.GetArrayLength() < 2)
                return Ok(new { success = false, message = "empty_route" });

            var result = new List<RoadPointDto>(coordinates.GetArrayLength());
            foreach (var c in coordinates.EnumerateArray())
            {
                if (c.GetArrayLength() < 2) continue;
                var lng = c[0].GetDouble();
                var lat = c[1].GetDouble();
                result.Add(new RoadPointDto(lat, lng));
            }

            return Json(new { success = result.Count >= 2, points = result });
        }
        catch
        {
            return Ok(new { success = false, message = "provider_failed" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> BuildRoadAlternatives([FromBody] RoadLegRequestDto? request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "invalid_request" });

        var coords =
            $"{request.FromLng.ToString("F6", CultureInfo.InvariantCulture)},{request.FromLat.ToString("F6", CultureInfo.InvariantCulture)};" +
            $"{request.ToLng.ToString("F6", CultureInfo.InvariantCulture)},{request.ToLat.ToString("F6", CultureInfo.InvariantCulture)}";
        var url = $"https://router.project-osrm.org/route/v1/driving/{coords}?alternatives=true&overview=full&geometries=geojson&steps=false";

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("BelarusHeritageRouteBuilder/1.0");
            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, new { success = false, message = "provider_unavailable" });

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            if (!root.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                return Ok(new { success = false, message = "empty_route" });

            var result = new List<object>();
            foreach (var route in routes.EnumerateArray().Take(4))
            {
                var coordinates = route.GetProperty("geometry").GetProperty("coordinates");
                if (coordinates.GetArrayLength() < 2) continue;

                var points = new List<RoadPointDto>(coordinates.GetArrayLength());
                foreach (var c in coordinates.EnumerateArray())
                {
                    if (c.GetArrayLength() < 2) continue;
                    points.Add(new RoadPointDto(c[1].GetDouble(), c[0].GetDouble()));
                }

                result.Add(new
                {
                    DistanceMeters = route.TryGetProperty("distance", out var d) ? d.GetDouble() : 0,
                    DurationSeconds = route.TryGetProperty("duration", out var du) ? du.GetDouble() : 0,
                    Points = points
                });
            }

            return Json(new { success = result.Count > 0, routes = result });
        }
        catch
        {
            return Ok(new { success = false, message = "provider_failed" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SavePersonalCopyFromShare([FromBody] SavePersonalCopyRequestDto? request)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { success = false, message = "invalid_request" });

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var created = await _routeService.SavePersonalCopyFromPublicAsync(
            request.Token,
            userId,
            request.Title,
            request.StartAddress,
            ParseCoord(request.StartLat),
            ParseCoord(request.StartLng),
            request.EndAddress,
            ParseCoord(request.EndLat),
            ParseCoord(request.EndLng),
            request.OrderedObjectIds);

        if (created == null)
            return NotFound(new { success = false, message = "source_not_found" });

        return Json(new { success = true, routeId = created.Id });
    }

    public async Task<IActionResult> Share(string token)
    {
        var route = await _routeService.GetPublicRouteByTokenAsync(token);
        if (route == null)
            return NotFound();

        return View("Share", route);
    }

    public sealed record RoadPointDto(double Lat, double Lng);
    public sealed record RoadLegRequestDto(double FromLat, double FromLng, double ToLat, double ToLng);
    public sealed record SavePersonalCopyRequestDto(
        string Token,
        string? Title,
        string? StartAddress,
        string? StartLat,
        string? StartLng,
        string? EndAddress,
        string? EndLat,
        string? EndLng,
        List<int>? OrderedObjectIds);
}
