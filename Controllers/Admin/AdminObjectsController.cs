using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BelarusHeritage.Controllers.Admin;

[Authorize(Policy = "Admin")]
public class AdminObjectsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ObjectService _objectService;
    private readonly FileService _fileService;

    public AdminObjectsController(AppDbContext context, ObjectService objectService, FileService fileService)
    {
        _context = context;
        _objectService = objectService;
        _fileService = fileService;
    }

    public async Task<IActionResult> Index(
        string? status = null,
        int? regionId = null,
        int? categoryId = null,
        string? search = null,
        int page = 1)
    {
        var query = _context.HeritageObjects
            .Include(o => o.Category)
            .Include(o => o.Region)
            .Include(o => o.Images.Where(i => i.IsMain))
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ObjectStatus>(status, true, out var s))
            query = query.Where(o => o.Status == s);

        if (regionId.HasValue)
            query = query.Where(o => o.RegionId == regionId.Value);

        if (categoryId.HasValue)
            query = query.Where(o => o.CategoryId == categoryId.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(o => o.NameRu.Contains(search) || o.NameEn.Contains(search));

        var totalCount = await query.CountAsync();

        var model = new AdminObjectsViewModel
        {
            Objects = await query
                .OrderByDescending(o => o.UpdatedAt)
                .Skip((page - 1) * 20)
                .Take(20)
                .ToListAsync(),
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = 20,
            Status = status,
            RegionId = regionId,
            CategoryId = categoryId,
            Search = search
        };

        ViewBag.Regions = await _context.Regions.ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Regions = await _context.Regions.ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.ArchStyles = await _context.ArchStyles.ToListAsync();
        ViewBag.Tags = await _context.Tags.ToListAsync();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(HeritageObject model, List<int>? selectedTags, List<IFormFile>? imageFiles)
    {
        ApplyLocationFromForm(model);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        await _objectService.CreateObjectAsync(model, userId);

        if (selectedTags != null)
        {
            foreach (var tagId in selectedTags)
            {
                _context.ObjectTagMaps.Add(new ObjectTagMap
                {
                    ObjectId = model.Id,
                    TagId = tagId
                });
            }
            await _context.SaveChangesAsync();
        }

        if (imageFiles != null && imageFiles.Count > 0)
        {
            byte sortOrder = 0;
            var hasMainImage = false;

            foreach (var file in imageFiles.Where(f => f != null && f.Length > 0))
            {
                var result = await _fileService.UploadImageAsync(file, "objects", model.Id);

                var image = new ObjectImage
                {
                    ObjectId = model.Id,
                    Url = "/" + result.Url,
                    ThumbUrl = result.ThumbUrl != null ? "/" + result.ThumbUrl : null,
                    UploadedBy = userId,
                    SortOrder = sortOrder++,
                    IsMain = !hasMainImage
                };

                if (!hasMainImage)
                {
                    model.MainImageUrl = image.Url;
                    hasMainImage = true;
                }

                _context.ObjectImages.Add(image);
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var obj = await _context.HeritageObjects
            .Include(o => o.TagMaps)
            .Include(o => o.Location)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (obj == null)
            return NotFound();

        ViewBag.Regions = await _context.Regions.ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.ArchStyles = await _context.ArchStyles.ToListAsync();
        ViewBag.Tags = await _context.Tags.ToListAsync();
        ViewBag.Images = await _context.ObjectImages.Where(i => i.ObjectId == id).OrderBy(i => i.SortOrder).ToListAsync();

        return View(obj);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(HeritageObject model, List<int>? selectedTags)
    {
        ApplyLocationFromForm(model);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        await _objectService.UpdateObjectAsync(model, userId);

        // Update tags
        var existingTags = await _context.ObjectTagMaps.Where(t => t.ObjectId == model.Id).ToListAsync();
        _context.ObjectTagMaps.RemoveRange(existingTags);

        if (selectedTags != null)
        {
            foreach (var tagId in selectedTags)
            {
                _context.ObjectTagMaps.Add(new ObjectTagMap
                {
                    ObjectId = model.Id,
                    TagId = tagId
                });
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromBody] int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _objectService.SoftDeleteObjectAsync(id, userId);

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Publish(int id)
    {
        var obj = await _context.HeritageObjects.FindAsync(id);
        if (obj != null)
        {
            obj.Status = ObjectStatus.Published;
            obj.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Unpublish(int id)
    {
        var obj = await _context.HeritageObjects.FindAsync(id);
        if (obj != null)
        {
            obj.Status = ObjectStatus.Draft;
            obj.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    public record MassActionRequest(string Action, List<int> Ids);

    [HttpPost]
    public async Task<IActionResult> MassAction([FromBody] MassActionRequest req)
    {
        var (action, ids) = (req.Action, req.Ids);
        var objects = await _context.HeritageObjects.Where(o => ids.Contains(o.Id)).ToListAsync();

        switch (action)
        {
            case "publish":
                foreach (var obj in objects)
                {
                    obj.Status = ObjectStatus.Published;
                    obj.UpdatedAt = DateTime.UtcNow;
                }
                break;
            case "unpublish":
                foreach (var obj in objects)
                {
                    obj.Status = ObjectStatus.Draft;
                    obj.UpdatedAt = DateTime.UtcNow;
                }
                break;
            case "delete":
                foreach (var obj in objects)
                {
                    obj.IsDeleted = true;
                    obj.Status = ObjectStatus.Draft;
                    obj.UpdatedAt = DateTime.UtcNow;
                }
                break;
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile file, int objectId)
    {
        try
        {
            var result = await _fileService.UploadImageAsync(file, "objects", objectId);
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

            var image = new ObjectImage
            {
                ObjectId = objectId,
                Url = "/" + result.Url,
                ThumbUrl = result.ThumbUrl != null ? "/" + result.ThumbUrl : null,
                UploadedBy = userId
            };
            _context.ObjectImages.Add(image);
            await _context.SaveChangesAsync();

            return Json(new { success = true, imageId = image.Id, thumbUrl = image.ThumbUrl ?? image.Url });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public record SetMainImageRequest(int ImageId, int ObjectId);

    [HttpPost]
    public async Task<IActionResult> SetMainImage([FromBody] SetMainImageRequest req)
    {
        var images = await _context.ObjectImages.Where(i => i.ObjectId == req.ObjectId).ToListAsync();
        foreach (var img in images)
            img.IsMain = img.Id == req.ImageId;

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImage([FromBody] int imageId)
    {
        var image = await _context.ObjectImages.FindAsync(imageId);
        if (image != null)
        {
            _context.ObjectImages.Remove(image);
            await _context.SaveChangesAsync();
        }
        return Json(new { success = true });
    }

    private void ApplyLocationFromForm(HeritageObject model)
    {
        // Parse map coordinates explicitly to avoid culture-dependent decimal binding issues.
        var latRaw = (Request.Form["Location.Lat"].FirstOrDefault() ?? string.Empty).Trim();
        var lngRaw = (Request.Form["Location.Lng"].FirstOrDefault() ?? string.Empty).Trim();

        if (!TryParseDecimalFlexible(latRaw, out var lat) || !TryParseDecimalFlexible(lngRaw, out var lng))
        {
            model.Location = null;
            return;
        }

        // Ignore unset/default pair (common when map tab wasn't used).
        if (lat == 0m && lng == 0m)
        {
            model.Location = null;
            return;
        }

        model.Location ??= new ObjectLocation();
        model.Location.Lat = lat;
        model.Location.Lng = lng;
        model.Location.AddressRu = (Request.Form["Location.AddressRu"].FirstOrDefault() ?? string.Empty).Trim();
        model.Location.AddressBe = (Request.Form["Location.AddressBe"].FirstOrDefault() ?? string.Empty).Trim();
        model.Location.AddressEn = (Request.Form["Location.AddressEn"].FirstOrDefault() ?? string.Empty).Trim();
        if (model.Location.MapZoom == 0)
            model.Location.MapZoom = 15;
    }

    private static bool TryParseDecimalFlexible(string raw, out decimal value)
    {
        raw = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = 0m;
            return false;
        }

        // Try both decimal separators and cultures.
        var normalized = raw.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
               || decimal.TryParse(raw, NumberStyles.Float, CultureInfo.GetCultureInfo("ru-RU"), out value)
               || decimal.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    }
}
