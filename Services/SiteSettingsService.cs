using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BelarusHeritage.Services;

public class SiteSettingsService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "site_settings_all";

    public SiteSettingsService(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<string?> GetAsync(string key)
    {
        var all = await GetAllAsync();
        return all.TryGetValue(key, out var val) ? val : null;
    }

    public async Task SetAsync(string key, string? value)
    {
        var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new SiteSettings { Key = key };
            _context.SiteSettings.Add(setting);
        }
        setting.Value = value;
        setting.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _cache.Remove(CacheKey);
    }

    public async Task<Dictionary<string, string?>> GetSocialLinksAsync()
    {
        var all = await GetAllAsync();
        var keys = new[] { "social.telegram", "social.instagram", "social.vk", "social.youtube" };
        return keys.ToDictionary(k => k, k => all.TryGetValue(k, out var v) ? v : null);
    }

    private async Task<Dictionary<string, string?>> GetAllAsync()
    {
        if (_cache.TryGetValue<Dictionary<string, string?>>(CacheKey, out var cached) && cached != null)
            return cached;

        var settings = await _context.SiteSettings.ToListAsync();
        var dict = settings.ToDictionary(s => s.Key, s => s.Value);
        _cache.Set(CacheKey, dict, TimeSpan.FromMinutes(10));
        return dict;
    }
}
