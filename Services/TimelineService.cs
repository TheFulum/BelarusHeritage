using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Services;

public class TimelineService
{
    private readonly AppDbContext _context;

    public TimelineService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TimelineEvent>> GetPublishedEventsAsync()
    {
        return await _context.TimelineEvents
            .Include(e => e.Object)
            .ThenInclude(o => o!.Images)
            .Where(e => e.IsPublished)
            .OrderBy(e => e.Year)
            .ThenBy(e => e.SortOrder)
            .ToListAsync();
    }

    public async Task<List<TimelineEvent>> GetEventsByYearAsync(short year)
    {
        return await _context.TimelineEvents
            .Include(e => e.Object)
            .Where(e => e.IsPublished && e.Year == year)
            .OrderBy(e => e.SortOrder)
            .ToListAsync();
    }

    public async Task<TimelineEvent?> GetEventAsync(int eventId)
    {
        return await _context.TimelineEvents
            .Include(e => e.Object)
            .ThenInclude(o => o!.Category)
            .FirstOrDefaultAsync(e => e.Id == eventId);
    }

    public async Task<List<TimelineEvent>> GetTodayInHistoryAsync()
    {
        // For now, just return recent events
        return await _context.TimelineEvents
            .Include(e => e.Object)
            .ThenInclude(o => o!.Images)
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.Year)
            .Take(3)
            .ToListAsync();
    }

    public async Task<List<TimelineEvent>> GetUpcomingEventsAsync(int count = 3)
    {
        var currentYear = (short)DateTime.UtcNow.Year;
        var minYear = (short)(currentYear - 120);

        return await _context.TimelineEvents
            .Include(e => e.Object)
            .ThenInclude(o => o!.Images)
            .Where(e => e.IsPublished && e.Year >= minYear && e.Year <= currentYear + 50)
            .OrderBy(e => e.Year)
            .ThenBy(e => e.SortOrder)
            .Take(count)
            .ToListAsync();
    }

    public async Task<TimelineEvent> CreateEventAsync(TimelineEvent evt)
    {
        TimelineEventNormalizer.Normalize(evt);
        _context.TimelineEvents.Add(evt);
        await _context.SaveChangesAsync();
        return evt;
    }

    public async Task<TimelineEvent> UpdateEventAsync(TimelineEvent evt)
    {
        var existing = await _context.TimelineEvents.FindAsync(evt.Id);
        if (existing == null)
            throw new InvalidOperationException($"TimelineEvent {evt.Id} not found");

        TimelineEventNormalizer.ApplyPosted(existing, evt);
        TimelineEventNormalizer.Normalize(existing);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteEventAsync(int eventId)
    {
        var evt = await _context.TimelineEvents.FindAsync(eventId);
        if (evt != null)
        {
            _context.TimelineEvents.Remove(evt);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<short>> GetYearsWithEventsAsync()
    {
        return await _context.TimelineEvents
            .Where(e => e.IsPublished)
            .Select(e => e.Year)
            .Distinct()
            .OrderBy(y => y)
            .ToListAsync();
    }
}
