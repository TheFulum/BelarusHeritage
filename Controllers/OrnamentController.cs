using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Services;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers;

public class OrnamentController : Controller
{
    private readonly AppDbContext _context;
    private readonly FileService _fileService;

    public OrnamentController(AppDbContext context, FileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    public IActionResult Builder()
    {
        return View();
    }

    public async Task<IActionResult> Index(string? q, string sortBy = "newest", int page = 1)
    {
        const int pageSize = 24;

        var query = _context.Ornaments
            .Where(o => o.IsPublic)
            .Include(o => o.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(o => o.Title != null && o.Title.Contains(q));

        query = sortBy == "oldest"
            ? query.OrderBy(o => o.CreatedAt)
            : query.OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync();
        var ornaments = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return View(new OrnamentIndexViewModel
        {
            Ornaments = ornaments,
            TotalCount = total,
            CurrentPage = page,
            PageSize = pageSize,
            SearchQuery = q,
            SortBy = sortBy
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveOrnament(string title, string imageData)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var imageUrl = _fileService.GetOrnamentImage(imageData);

        var ornament = new Ornament
        {
            UserId = userId,
            Title = title,
            ImageUrl = imageUrl,
            IsPublic = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Ornaments.Add(ornament);
        await _context.SaveChangesAsync();

        return Json(new { success = true, ornamentId = ornament.Id });
    }

    [HttpGet]
    public async Task<IActionResult> GetUserOrnaments()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var ornaments = await _context.Ornaments
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Json(ornaments);
    }

    [HttpPost]
    public async Task<IActionResult> TogglePublic(int ornamentId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var ornament = await _context.Ornaments
            .FirstOrDefaultAsync(o => o.Id == ornamentId && o.UserId == userId);

        if (ornament == null)
            return NotFound();

        ornament.IsPublic = !ornament.IsPublic;
        await _context.SaveChangesAsync();

        return Json(new { success = true, isPublic = ornament.IsPublic });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteOrnament(int ornamentId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var ornament = await _context.Ornaments
            .FirstOrDefaultAsync(o => o.Id == ornamentId && o.UserId == userId);

        if (ornament == null)
            return NotFound();

        _fileService.DeleteFile(ornament.ImageUrl);
        _context.Ornaments.Remove(ornament);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }
}
