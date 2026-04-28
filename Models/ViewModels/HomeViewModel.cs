using HeritageRoute = BelarusHeritage.Models.Domain.Route;
using Microsoft.AspNetCore.Mvc;
using BelarusHeritage.Models.Domain;

namespace BelarusHeritage.Models.ViewModels;

public class HomeViewModel
{
    public int TotalObjects { get; set; }
    public int TotalRegions { get; set; }
    public int TotalPhotos { get; set; }
    public int TotalUsers { get; set; }
    public List<HeritageObject> FeaturedObjects { get; set; } = new();
    public List<CategoryWithCount> Categories { get; set; } = new();
    public List<TimelineEvent> TodayInHistory { get; set; } = new();
    public List<TimelineEvent> TimelinePreview { get; set; } = new();
    public List<Quiz> Quizzes { get; set; } = new();
    public List<HeritageRoute> PublicRoutes { get; set; } = new();
}

public class CategoryWithCount
{
    public Category Category { get; set; } = null!;
    public int ObjectCount { get; set; }
}
