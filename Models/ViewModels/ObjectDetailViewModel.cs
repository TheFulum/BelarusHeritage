using BelarusHeritage.Models.Domain;

namespace BelarusHeritage.Models.ViewModels;

public class ObjectDetailViewModel
{
    public HeritageObject Object { get; set; } = null!;

    public List<ObjectImage> Gallery { get; set; } = new();
    public List<Tag> Tags { get; set; } = new();
    public List<HeritageObject> RelatedObjects { get; set; } = new();
    public List<HeritageObject> NearbyObjects { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<ObjectSource> Sources { get; set; } = new();

    public int? CurrentUserRating { get; set; }
    public bool IsFavorite { get; set; }
    public int CommentsCount { get; set; }
    public int TotalComments { get; set; }
    public int CurrentCommentsPage { get; set; }
    public int TotalCommentsPages { get; set; }
}
