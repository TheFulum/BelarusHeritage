using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BelarusHeritage.Data;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Services;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers;

public class ObjectController : Controller
{
    private readonly AppDbContext _context;
    private readonly ObjectService _objectService;
    private readonly RatingService _ratingService;
    private readonly SearchService _searchService;

    public ObjectController(
        AppDbContext context,
        ObjectService objectService,
        RatingService ratingService,
        SearchService searchService)
    {
        _context = context;
        _objectService = objectService;
        _ratingService = ratingService;
        _searchService = searchService;
    }

    public async Task<IActionResult> Detail(string slug)
    {
        var lang = Request.Cookies["culture"] ?? "ru";
        var obj = await _objectService.GetObjectBySlugAsync(slug);

        if (obj == null)
            return NotFound();

        var userId = User.Identity?.IsAuthenticated == true
            ? int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value)
            : 0;

        // Get comments (paginated - 10 per page)
        var commentsPage = 1;
        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.ObjectId == obj.Id && c.Status == CommentStatus.Approved && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((commentsPage - 1) * 10)
            .Take(10)
            .ToListAsync();

        var totalComments = await _context.Comments
            .CountAsync(c => c.ObjectId == obj.Id && c.Status == CommentStatus.Approved && !c.IsDeleted);

        // Get related and nearby objects
        var related = await _objectService.GetRelatedObjectsAsync(obj.Id, 5);
        var nearby = obj.Location != null
            ? await _objectService.GetNearbyObjectsAsync(obj.Id, obj.Location.Lat, obj.Location.Lng, 30, 5)
            : new List<HeritageObject>();

        var model = new ObjectDetailViewModel
        {
            Object = obj,
            Gallery = obj.Images.ToList(),
            Tags = obj.TagMaps.Select(t => t.Tag!).ToList(),
            RelatedObjects = related,
            NearbyObjects = nearby.Where(o => !related.Any(r => r.Id == o.Id)).Take(5).ToList(),
            Comments = comments,
            Sources = obj.Sources.ToList(),
            CurrentUserRating = userId > 0 ? (await _ratingService.GetUserRatingAsync(obj.Id, userId))?.Value : null,
            IsFavorite = userId > 0 ? await _context.Favorites.AnyAsync(f => f.UserId == userId && f.ObjectId == obj.Id) : false,
            CommentsCount = comments.Count,
            TotalComments = totalComments,
            CurrentCommentsPage = commentsPage,
            TotalCommentsPages = (int)Math.Ceiling(totalComments / 10.0)
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SetRating(int objectId, byte value)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _ratingService.SetRatingAsync(objectId, userId, value);

        return Json(new { success = true });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ToggleFavorite(int objectId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var existing = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ObjectId == objectId);

        if (existing != null)
        {
            _context.Favorites.Remove(existing);
            await _context.SaveChangesAsync();
            return Json(new { success = true, isFavorite = false });
        }
        else
        {
            _context.Favorites.Add(new Favorite
            {
                UserId = userId,
                ObjectId = objectId,
                AddedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, isFavorite = true });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddComment(int objectId, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return Json(new { success = false, message = "Comment cannot be empty" });

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var comment = new Comment
        {
            UserId = userId,
            ObjectId = objectId,
            Body = body,
            Status = CommentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Comment submitted for moderation" });
    }

    [HttpPost]
    public async Task<IActionResult> ReportError(int objectId, string subject, string description)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Save error report to audit log
        // This is a simplified implementation
        return Json(new { success = true });
    }
}
