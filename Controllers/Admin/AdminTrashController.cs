using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Services;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers.Admin;

[Authorize(Policy = "Admin")]
public class AdminTrashController : Controller
{
    private readonly AppDbContext _context;
    private readonly ObjectService _objectService;

    public AdminTrashController(AppDbContext context, ObjectService objectService)
    {
        _context = context;
        _objectService = objectService;
    }

    public async Task<IActionResult> Index()
    {
        var deletedObjects = await _context.HeritageObjects
            .Include(o => o.Category)
            .Include(o => o.Region)
            .Where(o => o.IsDeleted)
            .OrderByDescending(o => o.UpdatedAt)
            .ToListAsync();

        var model = new AdminTrashViewModel
        {
            Objects = deletedObjects,
            TotalCount = deletedObjects.Count
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Restore([FromBody] int id)
    {
        await _objectService.RestoreObjectAsync(id);
        return Json(new { success = true });
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> DeleteForever([FromBody] int id)
    {
        var obj = await _context.HeritageObjects.FindAsync(id);
        if (obj != null)
        {
            _context.HeritageObjects.Remove(obj);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> MassRestore([FromBody] List<int> ids)
    {
        foreach (var id in ids)
        {
            await _objectService.RestoreObjectAsync(id);
        }

        return Json(new { success = true });
    }
}
