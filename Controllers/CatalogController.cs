using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Services;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers;

public class CatalogController : Controller
{
    private readonly AppDbContext _context;
    private readonly ObjectService _objectService;

    public CatalogController(AppDbContext context, ObjectService objectService)
    {
        _context = context;
        _objectService = objectService;
    }

    public async Task<IActionResult> Index(
        int? category = null,
        int? region = null,
        int? tag = null,
        short? centuryStart = null,
        short? centuryEnd = null,
        string? status = null,
        bool? hasImages = null,
        bool? visitable = null,
        string sortBy = "name",
        int page = 1)
    {
        var lang = Request.Cookies["culture"] ?? "ru";

        PreservationStatus? preservationStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PreservationStatus>(status, true, out var ps))
            preservationStatus = ps;

        var objects = await _objectService.GetObjectsAsync(
            category, region, tag, centuryStart, centuryEnd,
            preservationStatus, hasImages, visitable, sortBy, page);

        var totalCount = await _objectService.GetObjectsCountAsync(category, region, tag);

        var model = new CatalogViewModel
        {
            Objects = objects,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = 12,
            SortBy = sortBy,
            Filters = new CatalogFilter
            {
                CategoryId = category,
                RegionId = region,
                TagId = tag,
                CenturyStart = centuryStart,
                CenturyEnd = centuryEnd,
                PreservationStatus = preservationStatus,
                HasImages = hasImages,
                Visitable = visitable
            },
            Categories = await _context.Categories.ToListAsync(),
            Regions = await _context.Regions.ToListAsync(),
            Tags = await _context.Tags.ToListAsync()
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out var userId))
            {
            var favoriteIds = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.ObjectId)
                .ToListAsync();
            ViewData["FavoriteObjectIds"] = favoriteIds.ToHashSet();
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetFilteredObjects(
        int? category = null,
        int? region = null,
        int? tag = null,
        string sortBy = "name",
        int page = 1)
    {
        var objects = await _objectService.GetObjectsAsync(
            category, region, tag, null, null, null, null, null, sortBy, page);

        return PartialView("_ObjectCards", objects);
    }
}
