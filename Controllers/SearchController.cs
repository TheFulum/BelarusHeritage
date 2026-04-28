using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Services;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers;

public class SearchController : Controller
{
    private readonly SearchService _searchService;
    private readonly AppDbContext _context;

    public SearchController(SearchService searchService, AppDbContext context)
    {
        _searchService = searchService;
        _context = context;
    }

    [HttpGet("/search/{q?}", Name = "SearchPretty")]
    public async Task<IActionResult> Index(string? q, string? category = null, string? region = null, int page = 1)
    {
        // Backward compatibility: /Search?q=... -> /search/{q}
        if (string.IsNullOrWhiteSpace(q) && Request.Query.TryGetValue("q", out var queryQ) && !string.IsNullOrWhiteSpace(queryQ))
        {
            return Redirect(Url.RouteUrl("SearchPretty", new
            {
                q = queryQ.ToString(),
                category,
                region,
                page
            })!);
        }

        if (string.IsNullOrWhiteSpace(q))
            return RedirectToAction("Index", "Home");

        var objects = await _searchService.SearchWithFiltersAsync(q, category, region, null, page);
        var totalCount = await _searchService.GetSearchCountAsync(q, category, region);

        ViewBag.Query = q;
        ViewBag.Category = category;
        ViewBag.Region = region;
        ViewBag.TotalCount = totalCount;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / 12.0);
        ViewBag.Categories = await _searchService.GetCategoriesAsync();
        ViewBag.Regions = await _searchService.GetRegionsAsync();

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

        return View(objects);
    }

    [HttpGet]
    public async Task<IActionResult> Autocomplete(string q)
    {
        var lang = Request.Cookies["culture"] ?? "ru";
        var results = await _searchService.AutocompleteAsync(q, lang);

        return Json(results);
    }
}
