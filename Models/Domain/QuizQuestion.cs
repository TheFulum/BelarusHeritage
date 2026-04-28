using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class QuizQuestion
{
    public int Id { get; set; }

    [Required]
    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    [MaxLength(500)]
    public string? BodyRu { get; set; }
    [MaxLength(500)]
    public string? BodyBe { get; set; }
    [MaxLength(500)]
    public string? BodyEn { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public short SortOrder { get; set; } = 0;

    public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
}
