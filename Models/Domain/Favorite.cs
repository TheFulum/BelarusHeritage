using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class Favorite
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public int ObjectId { get; set; }
    public HeritageObject? Object { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
