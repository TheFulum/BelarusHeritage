using BelarusHeritage.Models.Domain;

namespace BelarusHeritage.Models.ViewModels;

public class OrnamentIndexViewModel
{
    public List<Ornament> Ornaments { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; } = 24;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? SearchQuery { get; set; }
    public string SortBy { get; set; } = "newest";
}
