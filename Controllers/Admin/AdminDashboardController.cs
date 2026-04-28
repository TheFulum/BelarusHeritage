using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Services;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers.Admin;

[Authorize(Policy = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly AuditLogService _auditLogService;

    public AdminDashboardController(AppDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel
        {
            TotalObjects = await _context.HeritageObjects.CountAsync(o => !o.IsDeleted),
            PublishedObjects = await _context.HeritageObjects.CountAsync(o => o.Status == ObjectStatus.Published && !o.IsDeleted),
            DraftObjects = await _context.HeritageObjects.CountAsync(o => o.Status == ObjectStatus.Draft && !o.IsDeleted),
            DeletedObjects = await _context.HeritageObjects.CountAsync(o => o.IsDeleted),
            TotalUsers = await _context.Users.CountAsync(),
            NewUsersLast30Days = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30)),
            PendingComments = await _context.Comments.CountAsync(c => c.Status == CommentStatus.Pending && !c.IsDeleted),
            TimelineEvents = await _context.TimelineEvents.CountAsync(e => e.IsPublished),
            RecentLogs = await _auditLogService.GetRecentLogsAsync(10)
        };

        // Objects by region
        var objectsByRegion = await _context.HeritageObjects
            .Where(o => !o.IsDeleted)
            .GroupBy(o => o.Region!.NameRu)
            .Select(g => new { Region = g.Key, Count = g.Count() })
            .ToListAsync();
        model.ObjectsByRegion = objectsByRegion.ToDictionary(x => x.Region, x => x.Count);

        // Objects by category
        var objectsByCategory = await _context.HeritageObjects
            .Where(o => !o.IsDeleted)
            .GroupBy(o => o.Category!.NameRu)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync();
        model.ObjectsByCategory = objectsByCategory.ToDictionary(x => x.Category, x => x.Count);

        return View(model);
    }
}
