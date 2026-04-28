using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Services;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers.Admin;

[Authorize(Policy = "Admin")]
public class AdminTimelineController : Controller
{
    private readonly AppDbContext _context;
    private readonly TimelineService _timelineService;
    private readonly FileService _fileService;

    public AdminTimelineController(AppDbContext context, TimelineService timelineService, FileService fileService)
    {
        _context = context;
        _timelineService = timelineService;
        _fileService = fileService;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _context.TimelineEvents
            .Include(e => e.Object)
            .OrderBy(e => e.Year)
            .ThenBy(e => e.SortOrder)
            .ToListAsync();

        return View(events);
    }

    public async Task<IActionResult> Create()
    {
        var objects = await _context.HeritageObjects
            .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted)
            .OrderBy(o => o.NameRu)
            .ToListAsync();

        ViewBag.Objects = objects;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(TimelineEvent model, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var result = await _fileService.UploadImageAsync(imageFile, "timeline");
            model.ImageUrl = "/" + result.Url;
        }
        await _timelineService.CreateEventAsync(model);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var evt = await _timelineService.GetEventAsync(id);
        if (evt == null) return NotFound();

        var objects = await _context.HeritageObjects
            .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted)
            .OrderBy(o => o.NameRu)
            .ToListAsync();

        ViewBag.Objects = objects;
        return View(evt);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(TimelineEvent model, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var result = await _fileService.UploadImageAsync(imageFile, "timeline");
            model.ImageUrl = "/" + result.Url;
        }
        await _timelineService.UpdateEventAsync(model);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromBody] int id)
    {
        await _timelineService.DeleteEventAsync(id);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> TogglePublish([FromBody] int id)
    {
        var evt = await _context.TimelineEvents.FindAsync(id);
        if (evt != null)
        {
            evt.IsPublished = !evt.IsPublished;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true, isPublished = evt?.IsPublished });
    }
}
