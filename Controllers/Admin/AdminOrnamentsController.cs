using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Localization;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers.Admin;

[Authorize(Policy = "Admin")]
public class AdminOrnamentsController : Controller
{
    private readonly AppDbContext _context;

    public AdminOrnamentsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? q = null, string visibility = "all", int page = 1)
    {
        const int pageSize = 24;

        var query = _context.Ornaments
            .Include(o => o.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(o =>
                (o.Title != null && o.Title.Contains(q)) ||
                (o.User != null && o.User.UserName != null && o.User.UserName.Contains(q)));
        }

        query = visibility switch
        {
            "public" => query.Where(o => o.IsPublic),
            "hidden" => query.Where(o => !o.IsPublic),
            _ => query
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = q;
        ViewBag.Visibility = visibility;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;

        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleVisibility([FromBody] int ornamentId)
    {
        var ornament = await _context.Ornaments.FirstOrDefaultAsync(o => o.Id == ornamentId);
        if (ornament == null)
            return Json(new { success = false, message = UiText.T(HttpContext, "admin.ornaments.index.notFound") });

        ornament.IsPublic = !ornament.IsPublic;
        await _context.SaveChangesAsync();

        return Json(new { success = true, isPublic = ornament.IsPublic });
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromBody] int ornamentId)
    {
        var ornament = await _context.Ornaments.FirstOrDefaultAsync(o => o.Id == ornamentId);
        if (ornament == null)
            return Json(new { success = false, message = UiText.T(HttpContext, "admin.ornaments.index.notFound") });

        _context.Ornaments.Remove(ornament);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }
}
