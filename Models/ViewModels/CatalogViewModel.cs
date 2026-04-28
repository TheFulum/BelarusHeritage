using BelarusHeritage.Models.Domain;

namespace BelarusHeritage.Models.ViewModels;

public class CatalogViewModel
{
    public List<HeritageObject> Objects { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public List<Category> Categories { get; set; } = new();
    public List<Region> Regions { get; set; } = new();
    public List<Tag> Tags { get; set; } = new();

    public CatalogFilter Filters { get; set; } = new();
    public string SortBy { get; set; } = "name";
}

public class CatalogFilter
{
    public int? CategoryId { get; set; }
    public int? RegionId { get; set; }
    public int? TagId { get; set; }
    public short? CenturyStart { get; set; }
    public short? CenturyEnd { get; set; }
    public PreservationStatus? PreservationStatus { get; set; }
    public bool? HasImages { get; set; }
    public bool? Visitable { get; set; }
}
