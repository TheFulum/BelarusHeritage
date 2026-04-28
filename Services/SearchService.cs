using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Services;

public class SearchService
{
    private readonly AppDbContext _context;

    public SearchService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<HeritageObject>> SearchAsync(string query, string lang = "ru")
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<HeritageObject>();

        query = query.Trim().ToLower();

        return await _context.HeritageObjects
            .Include(o => o.Category)
            .Include(o => o.Region)
            .Include(o => o.Location)
            .Include(o => o.Images.Where(i => i.IsMain))
            .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted)
            .Where(o => lang == "ru" ?
                o.NameRu!.ToLower().Contains(query) ||
                o.NameBe!.ToLower().Contains(query) ||
                o.ShortDescRu!.ToLower().Contains(query) :
                o.NameEn!.ToLower().Contains(query) ||
                o.ShortDescEn!.ToLower().Contains(query))
            .OrderByDescending(o => o.RatingAvg)
            .Take(20)
            .ToListAsync();
    }

    public async Task<List<AutocompleteResult>> AutocompleteAsync(string query, string lang = "ru")
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<AutocompleteResult>();

        query = query.Trim().ToLower();

        var objects = await _context.HeritageObjects
            .Include(o => o.Category)
            .Include(o => o.Region)
            .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted)
            .Where(o => lang == "ru" ?
                o.NameRu!.ToLower().Contains(query) || o.NameBe!.ToLower().Contains(query) :
                o.NameEn!.ToLower().Contains(query))
            .Take(5)
            .ToListAsync();

        return objects.Select(o => new AutocompleteResult
        {
            Id = o.Id,
            Slug = o.Slug,
            Name = lang == "ru" ? o.NameRu : lang == "be" ? o.NameBe : o.NameEn,
            Category = o.Category != null ? (lang == "ru" ? o.Category.NameRu : lang == "be" ? o.Category.NameBe : o.Category.NameEn) : null,
            IconClass = o.Category?.IconClass
        }).ToList();
    }

    public async Task<List<HeritageObject>> SearchWithFiltersAsync(
        string query,
        string? categorySlug = null,
        string? regionCode = null,
        int? tagId = null,
        int page = 1,
        int pageSize = 12)
    {
        var objectsQuery = _context.HeritageObjects
            .Include(o => o.Category)
            .Include(o => o.Region)
            .Include(o => o.Location)
            .Include(o => o.Images.Where(i => i.IsMain))
            .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.ToLower();
            objectsQuery = objectsQuery.Where(o =>
                o.NameRu!.ToLower().Contains(query) ||
                o.NameBe!.ToLower().Contains(query) ||
                o.NameEn!.ToLower().Contains(query) ||
                (o.ShortDescRu != null && o.ShortDescRu.ToLower().Contains(query)));
        }

        if (!string.IsNullOrWhiteSpace(categorySlug))
            objectsQuery = objectsQuery.Where(o => o.Category!.Slug == categorySlug);

        if (!string.IsNullOrWhiteSpace(regionCode))
            objectsQuery = objectsQuery.Where(o => o.Region!.Code == regionCode);

        if (tagId.HasValue)
            objectsQuery = objectsQuery.Where(o => o.TagMaps.Any(t => t.TagId == tagId.Value));

        return await objectsQuery
            .OrderByDescending(o => o.RatingAvg)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetSearchCountAsync(string query, string? categorySlug = null, string? regionCode = null)
    {
        var objectsQuery = _context.HeritageObjects
            .Where(o => o.Status == ObjectStatus.Published && !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.ToLower();
            objectsQuery = objectsQuery.Where(o =>
                o.NameRu!.ToLower().Contains(query) ||
                o.NameBe!.ToLower().Contains(query) ||
                o.NameEn!.ToLower().Contains(query));
        }

        if (!string.IsNullOrWhiteSpace(categorySlug))
            objectsQuery = objectsQuery.Where(o => o.Category!.Slug == categorySlug);

        if (!string.IsNullOrWhiteSpace(regionCode))
            objectsQuery = objectsQuery.Where(o => o.Region!.Code == regionCode);

        return await objectsQuery.CountAsync();
    }

    public async Task<List<Category>> GetCategoriesAsync()
        => await _context.Categories.OrderBy(c => c.NameRu).ToListAsync();

    public async Task<List<Region>> GetRegionsAsync()
        => await _context.Regions.OrderBy(r => r.NameRu).ToListAsync();
}

public class AutocompleteResult
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? IconClass { get; set; }
}

// Extension for MySQL full-text search in EF Core
public static class MySqlMatchSearchMode
{
    public const string Natural = "IN NATURAL LANGUAGE MODE";
    public const string Boolean = "IN BOOLEAN MODE";
    public const string QueryExpansion = "WITH QUERY EXPANSION";
}
