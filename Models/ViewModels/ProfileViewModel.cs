using BelarusHeritage.Models.Domain;
using HeritageRoute = BelarusHeritage.Models.Domain.Route;

namespace BelarusHeritage.Models.ViewModels;

public class ProfileViewModel
{
    public User User { get; set; } = null!;
    public List<HeritageObject> Favorites { get; set; } = new();
    public List<CommentWithObject> Comments { get; set; } = new();
    public List<RatingWithObject> Ratings { get; set; } = new();
    public List<HeritageRoute> Routes { get; set; } = new();
    public List<Ornament> Ornaments { get; set; } = new();
    public List<QuizResult> QuizResults { get; set; } = new();
}

public class CommentWithObject
{
    public Comment Comment { get; set; } = null!;
    public HeritageObject Object { get; set; } = null!;
}

public class RatingWithObject
{
    public Rating Rating { get; set; } = null!;
    public HeritageObject Object { get; set; } = null!;
}
