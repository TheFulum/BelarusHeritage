using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Data;
using BelarusHeritage.Localization;
using BelarusHeritage.Models.Domain;
using BelarusHeritage.Services;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Controllers.Admin;

[Authorize(Policy = "Admin")]
public class AdminCatalogDataController : Controller
{
    private readonly AppDbContext _context;

    public AdminCatalogDataController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    #region Categories

    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Categories.OrderBy(c => c.NameRu).ToListAsync();
        return Json(categories);
    }

    [HttpPost]
    public async Task<IActionResult> SaveCategory([FromBody] Category model)
    {
        NormalizeSlugEntity(model, maxSlugLength: 60);
        if (string.IsNullOrWhiteSpace(model.NameRu))
            return Json(new { success = false, message = UiText.T(HttpContext, "admin.catalog.validation.nameRuRequired") });

        if (model.Id == 0)
            _context.Categories.Add(model);
        else
            _context.Categories.Update(model);

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory([FromBody] int id)
    {
        var hasObjects = await _context.HeritageObjects.AnyAsync(o => o.CategoryId == id);
        if (hasObjects)
            return Json(new { success = false, message = UiText.T(HttpContext, "admin.catalog.index.delete.categoryHasObjects") });

        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    #endregion

    #region Tags

    public async Task<IActionResult> GetTags()
    {
        var tags = await _context.Tags.OrderBy(t => t.NameRu).ToListAsync();
        return Json(tags);
    }

    [HttpPost]
    public async Task<IActionResult> SaveTag([FromBody] Tag model)
    {
        NormalizeSlugEntity(model, maxSlugLength: 80);
        if (string.IsNullOrWhiteSpace(model.NameRu))
            return Json(new { success = false, message = UiText.T(HttpContext, "admin.catalog.validation.nameRuRequired") });

        if (model.Id == 0)
            _context.Tags.Add(model);
        else
            _context.Tags.Update(model);

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTag([FromBody] int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag != null)
        {
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    #endregion

    #region ArchStyles

    public async Task<IActionResult> GetArchStyles()
    {
        var styles = await _context.ArchStyles.OrderBy(s => s.NameRu).ToListAsync();
        return Json(styles);
    }

    [HttpPost]
    public async Task<IActionResult> SaveArchStyle([FromBody] ArchStyle model)
    {
        NormalizeSlugEntity(model, maxSlugLength: 60);
        if (string.IsNullOrWhiteSpace(model.NameRu))
            return Json(new { success = false, message = UiText.T(HttpContext, "admin.catalog.validation.nameRuRequired") });

        if (model.Id == 0)
            _context.ArchStyles.Add(model);
        else
            _context.ArchStyles.Update(model);

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteArchStyle([FromBody] int id)
    {
        var style = await _context.ArchStyles.FindAsync(id);
        if (style != null)
        {
            _context.ArchStyles.Remove(style);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    #endregion

    #region Regions

    public async Task<IActionResult> GetRegions()
    {
        var regions = await _context.Regions.OrderBy(r => r.NameRu).ToListAsync();
        return Json(regions);
    }

    [HttpPost]
    public async Task<IActionResult> SaveRegion([FromBody] Region model)
    {
        var ru = model.NameRu;
        var be = model.NameBe;
        var en = model.NameEn;
        LocalizedNameNormalizer.FillNameLocales(ref ru, ref be, ref en);

        if (string.IsNullOrWhiteSpace(ru))
            return Json(new { success = false, message = UiText.T(HttpContext, "admin.catalog.validation.nameRuRequired") });

        var existing = await _context.Regions.FindAsync(model.Id);
        if (existing != null)
        {
            existing.NameRu = ru;
            existing.NameBe = be;
            existing.NameEn = en;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    private static void NormalizeSlugEntity(Category model, int maxSlugLength) =>
        NormalizeSlugEntity(model.NameRu, model.NameBe, model.NameEn, model.Slug, maxSlugLength,
            (ru, be, en, slug) => { model.NameRu = ru; model.NameBe = be; model.NameEn = en; model.Slug = slug; });

    private static void NormalizeSlugEntity(Tag model, int maxSlugLength) =>
        NormalizeSlugEntity(model.NameRu, model.NameBe, model.NameEn, model.Slug, maxSlugLength,
            (ru, be, en, slug) => { model.NameRu = ru; model.NameBe = be; model.NameEn = en; model.Slug = slug; });

    private static void NormalizeSlugEntity(ArchStyle model, int maxSlugLength) =>
        NormalizeSlugEntity(model.NameRu, model.NameBe, model.NameEn, model.Slug, maxSlugLength,
            (ru, be, en, slug) => { model.NameRu = ru; model.NameBe = be; model.NameEn = en; model.Slug = slug; });

    private static void NormalizeSlugEntity(string nameRu, string nameBe, string nameEn, string slug, int maxSlugLength,
        Action<string, string, string, string> apply)
    {
        var ru = nameRu;
        var be = nameBe;
        var en = nameEn;
        var s = slug;
        LocalizedNameNormalizer.FillNameLocales(ref ru, ref be, ref en);
        LocalizedNameNormalizer.FillSlug(ref s, ru, maxSlugLength);
        apply(ru, be, en, s);
    }

    #endregion
}
