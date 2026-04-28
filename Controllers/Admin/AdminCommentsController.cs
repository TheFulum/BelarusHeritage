using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers.Admin;

[Authorize(Policy = "Admin")]
public class AdminCommentsController : Controller
{
    private readonly AppDbContext _context;

    public AdminCommentsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status = null, int? objectId = null, int? userId = null, int page = 1)
    {
        var query = _context.Comments
            .Include(c => c.User)
            .Include(c => c.Object)
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CommentStatus>(status, true, out var s))
            query = query.Where(c => c.Status == s);

        if (objectId.HasValue)
            query = query.Where(c => c.ObjectId == objectId.Value);

        if (userId.HasValue)
            query = query.Where(c => c.UserId == userId.Value);

        var model = new AdminCommentsViewModel
        {
            Comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * 20)
                .Take(20)
                .ToListAsync(),
            TotalCount = await query.CountAsync(),
            Status = status,
            ObjectId = objectId,
            UserId = userId
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Approve([FromBody] int id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            comment.Status = CommentStatus.Approved;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Reject([FromBody] int id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            comment.Status = CommentStatus.Rejected;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> MarkSpam([FromBody] int id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            comment.Status = CommentStatus.Spam;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromBody] int id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    public record CommentsMassRequest(string Action, List<int> Ids);

    [HttpPost]
    public async Task<IActionResult> MassAction([FromBody] CommentsMassRequest req)
    {
        var (action, ids) = (req.Action, req.Ids);
        var comments = await _context.Comments.Where(c => ids.Contains(c.Id)).ToListAsync();

        switch (action)
        {
            case "approve":
                foreach (var c in comments)
                {
                    c.Status = CommentStatus.Approved;
                    c.UpdatedAt = DateTime.UtcNow;
                }
                break;
            case "delete":
                foreach (var c in comments)
                {
                    c.IsDeleted = true;
                    c.UpdatedAt = DateTime.UtcNow;
                }
                break;
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }
}
