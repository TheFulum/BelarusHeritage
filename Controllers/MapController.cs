using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Services;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers;

public class MapController : Controller
{
    private readonly AppDbContext _context;

    public MapController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new MapViewModel
        {
            Categories = await _context.Categories.ToListAsync(),
            Regions = await _context.Regions.ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetMarkers(
        int? category = null,
        int? region = null,
        bool? visitable = null,
        string? culture = null)
    {
        var query = _context.HeritageObjects
            .Include(o => o.Category)
            .Include(o => o.Location)
            .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted)
            .Where(o => o.Location != null);

        if (category.HasValue)
            query = query.Where(o => o.CategoryId == category.Value);

        if (region.HasValue)
            query = query.Where(o => o.RegionId == region.Value);

        if (visitable == true)
            query = query.Where(o => o.IsVisitable);

        var lang = (culture ?? Request.Cookies["culture"] ?? "ru").ToLowerInvariant();
        if (lang is not ("ru" or "be" or "en")) lang = "ru";

        var markers = await query.ToListAsync();

        var result = markers.Select(o => new MapObjectMarker
        {
            Id = o.Id,
            Slug = o.Slug,
            Name = lang == "ru" ? o.NameRu : lang == "be" ? o.NameBe : o.NameEn,
            ImageUrl = o.MainImageUrl,
            CategorySlug = o.Category?.Slug ?? "",
            IconClass = o.Category?.IconClass,
            ColorHex = o.Category?.ColorHex,
            Lat = o.Location!.Lat,
            Lng = o.Location!.Lng,
            ShortDesc = lang == "ru" ? o.ShortDescRu : lang == "be" ? o.ShortDescBe : o.ShortDescEn
        }).ToList();

        return Json(result);
    }
}
