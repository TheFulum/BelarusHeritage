using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers;

public class ProfileController : Controller
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly RatingService _ratingService;
    private readonly RouteService _routeService;
    private readonly QuizService _quizService;
    private readonly FileService _fileService;

    public ProfileController(
        AppDbContext context,
        AuthService authService,
        RatingService ratingService,
        RouteService routeService,
        QuizService quizService,
        FileService fileService)
    {
        _context = context;
        _authService = authService;
        _ratingService = ratingService;
        _routeService = routeService;
        _quizService = quizService;
        _fileService = fileService;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
            return RedirectToAction("Login", "Auth");

        var favEntities = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Object).ThenInclude(o => o!.Category)
            .Include(f => f.Object).ThenInclude(o => o!.Images.Where(i => i.IsMain))
            .ToListAsync();

        var model = new ProfileViewModel
        {
            User = user,
            Favorites = favEntities.Select(f => f.Object!).Where(o => o != null).ToList(),
            Comments = await _context.Comments
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentWithObject
                {
                    Comment = c,
                    Object = c.Object!
                })
                .Take(20)
                .ToListAsync(),
            Ratings = (await _ratingService.GetUserRatingsAsync(userId))
                .Select(r => new RatingWithObject
                {
                    Rating = r,
                    Object = r.Object!
                })
                .ToList(),
            Routes = await _routeService.GetUserRoutesAsync(userId),
            Ornaments = await _context.Ornaments
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(),
            QuizResults = await _quizService.GetUserResultsAsync(userId)
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UpdateProfile(string? displayName, string? preferredLang)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _authService.UpdateProfileAsync(userId, displayName, preferredLang);

        return Json(new { success = true });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _authService.ChangePasswordAsync(userId, currentPassword, newPassword);

        return Json(new { success = result });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DeleteAccount()
    {
        // Admin-only action or user-initiated with confirmation
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _context.Users.FindAsync(userId);

        if (user != null)
        {
            user.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UploadAvatar(IFormFile avatar)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Json(new { success = false, error = "User not found" });

        try
        {
            var result = await _fileService.UploadImageAsync(avatar, "avatars");
            user.AvatarUrl = "/" + result.Url.Replace('\\', '/');
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Json(new { success = true, url = user.AvatarUrl });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> RemoveFavorite(int objectId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ObjectId == objectId);

        if (favorite != null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);

        if (comment != null)
        {
            comment.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ToggleRoutePublic(int routeId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var route = await _context.Routes.FirstOrDefaultAsync(r => r.Id == routeId && r.UserId == userId);

        if (route == null)
            return Json(new { success = false });

        route.IsPublic = !route.IsPublic;
        await _context.SaveChangesAsync();
        return Json(new { success = true, isPublic = route.IsPublic });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DeleteRoute(int routeId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var route = await _context.Routes
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.Id == routeId && r.UserId == userId);

        if (route == null)
            return Json(new { success = false });

        _context.Routes.Remove(route);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ToggleOrnamentPublic(int ornamentId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var ornament = await _context.Ornaments.FirstOrDefaultAsync(o => o.Id == ornamentId && o.UserId == userId);

        if (ornament == null)
            return Json(new { success = false });

        ornament.IsPublic = !ornament.IsPublic;
        await _context.SaveChangesAsync();
        return Json(new { success = true, isPublic = ornament.IsPublic });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DeleteOrnament(int ornamentId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var ornament = await _context.Ornaments.FirstOrDefaultAsync(o => o.Id == ornamentId && o.UserId == userId);

        if (ornament == null)
            return Json(new { success = false });

        _fileService.DeleteFile(ornament.ImageUrl.TrimStart('/'));
        _context.Ornaments.Remove(ornament);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
}
