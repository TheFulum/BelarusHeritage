using BelarusHeritage.Models.Domain;

namespace BelarusHeritage.Models.ViewModels;

public class MapViewModel
{
    public List<MapObjectMarker> Markers { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Region> Regions { get; set; } = new();
}

public class MapObjectMarker
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string CategorySlug { get; set; } = string.Empty;
    public string? IconClass { get; set; }
    public string? ColorHex { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public string? ShortDesc { get; set; }
}
