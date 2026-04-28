using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class Rating
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public int ObjectId { get; set; }
    public HeritageObject? Object { get; set; }

    [Range(1, 5)]
    public byte Value { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
