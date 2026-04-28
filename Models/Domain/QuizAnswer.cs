using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class QuizAnswer
{
    public int Id { get; set; }

    [Required]
    public int QuestionId { get; set; }
    public QuizQuestion? Question { get; set; }

    [Required]
    [MaxLength(300)]
    public string BodyRu { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string BodyBe { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string BodyEn { get; set; } = string.Empty;

    public bool IsCorrect { get; set; } = false;
    public byte SortOrder { get; set; } = 0;
}
