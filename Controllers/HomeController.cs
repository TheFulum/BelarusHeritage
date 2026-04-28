using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Services;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly ObjectService _objectService;
    private readonly TimelineService _timelineService;
    private readonly QuizService _quizService;
    private readonly RouteService _routeService;

    public HomeController(
        AppDbContext context,
        ObjectService objectService,
        TimelineService timelineService,
        QuizService quizService,
        RouteService routeService)
    {
        _context = context;
        _objectService = objectService;
        _timelineService = timelineService;
        _quizService = quizService;
        _routeService = routeService;
    }

    public async Task<IActionResult> Index()
    {
        var lang = GetCurrentLanguage();

        var model = new HomeViewModel
        {
            TotalObjects = await _context.HeritageObjects
                .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted)
                .CountAsync(),
            TotalRegions = await _context.Regions.CountAsync(),
            TotalPhotos = await _context.ObjectImages.CountAsync(),
            TotalUsers = await _context.Users.CountAsync(),
            FeaturedObjects = await _objectService.GetFeaturedObjectsAsync(8),
            Categories = await _context.Categories
                .Select(c => new CategoryWithCount
                {
                    Category = c,
                    ObjectCount = c.Objects.Count(o => o.Status == ObjectStatus.Published && !o.IsDeleted)
                })
                .ToListAsync(),
            TodayInHistory = await _timelineService.GetTodayInHistoryAsync(),
            TimelinePreview = await _timelineService.GetUpcomingEventsAsync(6),
            Quizzes = await _quizService.GetActiveQuizzesAsync(),
            PublicRoutes = await _routeService.GetPublicRoutesAsync(3)
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

    public IActionResult SetLanguage(string culture)
    {
        Response.Cookies.Append("culture", culture, new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddYears(1),
            HttpOnly = false
        });

        return Redirect(Request.Headers["Referer"].ToString());
    }

    private string GetCurrentLanguage()
    {
        return Request.Cookies["culture"] ?? "ru";
    }
}
