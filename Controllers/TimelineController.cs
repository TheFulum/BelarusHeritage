using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Localization;
using BelarusHeritage.Models.ViewModels;
using BelarusHeritage.Services;

namespace BelarusHeritage.Controllers;

public class TimelineController : Controller
{
    private readonly TimelineService _timelineService;

    public TimelineController(TimelineService timelineService)
    {
        _timelineService = timelineService;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _timelineService.GetPublishedEventsAsync();
        var years = await _timelineService.GetYearsWithEventsAsync();

        // Group by year
        var eventsByYear = events
            .GroupBy(e => e.Year)
            .ToDictionary(g => g.Key, g => g.ToList());

        var model = new TimelineViewModel
        {
            Events = events,
            Years = years,
            EventsByYear = eventsByYear,
            EpochDividers = GetEpochDividers(years)
        };

        return View(model);
    }

    private List<string> GetEpochDividers(List<short> years)
    {
        var dividers = new List<string>();
        string L(string key) => UiText.T(HttpContext, key);

        if (years.Any(y => y <= 1385))
            dividers.Add(L("timeline.epoch.gkl"));

        if (years.Any(y => y >= 1385 && y <= 1795))
            dividers.Add(L("timeline.epoch.rp"));

        if (years.Any(y => y >= 1795 && y <= 1917))
            dividers.Add(L("timeline.epoch.empire"));

        if (years.Any(y => y >= 1917 && y <= 1991))
            dividers.Add(L("timeline.epoch.bssr"));

        if (years.Any(y => y >= 1991))
            dividers.Add(L("timeline.epoch.rb"));

        return dividers;
    }
}
