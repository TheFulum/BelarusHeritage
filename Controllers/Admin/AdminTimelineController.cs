using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Localization;
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TimelineEvent model, IFormFile? imageFile)
    {
        TimelineEventNormalizer.Normalize(model);
        ClearTimelineBindingValidation();
        ValidateTimelineEvent(model);

        if (!ModelState.IsValid)
        {
            ViewBag.Objects = await GetPublishedObjectsAsync();
            return View(model);
        }

        if (imageFile != null && imageFile.Length > 0)
        {
            var result = await _fileService.UploadImageAsync(imageFile, "timeline");
            model.ImageUrl = "/" + result.Url;
        }

        await _timelineService.CreateEventAsync(model);
        TempData["Success"] = T("admin.timeline.validation.created");
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TimelineEvent model, IFormFile? imageFile)
    {
        TimelineEventNormalizer.Normalize(model);
        ClearTimelineBindingValidation();
        ValidateTimelineEvent(model);

        if (!ModelState.IsValid)
            return await TimelineFormErrorAsync(model);

        if (imageFile != null && imageFile.Length > 0)
        {
            var result = await _fileService.UploadImageAsync(imageFile, "timeline");
            model.ImageUrl = "/" + result.Url;
        }

        await _timelineService.UpdateEventAsync(model);
        TempData["Success"] = T("admin.timeline.validation.saved");
        return RedirectToAction(nameof(Edit), new { id = model.Id });
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

    private string T(string key) => UiText.T(HttpContext, key);

    private async Task<List<HeritageObject>> GetPublishedObjectsAsync() =>
        await _context.HeritageObjects
            .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted)
            .OrderBy(o => o.NameRu)
            .ToListAsync();

    private void ClearTimelineBindingValidation()
    {
        ModelState.Remove(nameof(TimelineEvent.TitleBe));
        ModelState.Remove(nameof(TimelineEvent.TitleEn));
    }

    private void ValidateTimelineEvent(TimelineEvent model)
    {
        if (string.IsNullOrWhiteSpace(model.TitleRu))
            ModelState.AddModelError(nameof(TimelineEvent.TitleRu), T("admin.timeline.validation.titleRuRequired"));
    }

    private async Task<IActionResult> TimelineFormErrorAsync(TimelineEvent posted)
    {
        ViewBag.Objects = await GetPublishedObjectsAsync();

        if (posted.Id == 0)
            return View("Create", posted);

        var existing = await _timelineService.GetEventAsync(posted.Id);
        if (existing == null)
            return NotFound();

        TimelineEventNormalizer.ApplyPosted(existing, posted);
        if (!string.IsNullOrWhiteSpace(posted.ImageUrl))
            existing.ImageUrl = posted.ImageUrl;

        return View("Edit", existing);
    }
}
