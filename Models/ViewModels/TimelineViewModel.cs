using BelarusHeritage.Models.Domain;

namespace BelarusHeritage.Models.ViewModels;

public class TimelineViewModel
{
    public List<TimelineEvent> Events { get; set; } = new();
    public List<short> Years { get; set; } = new();
    public Dictionary<short, List<TimelineEvent>> EventsByYear { get; set; } = new();
    public List<string> EpochDividers { get; set; } = new();
}
