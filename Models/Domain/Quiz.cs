using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class Quiz
{
    public int Id { get; set; }

    [Required]
    [MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    public QuizType Type { get; set; }

    [Required]
    [MaxLength(200)]
    public string TitleRu { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string TitleBe { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string TitleEn { get; set; } = string.Empty;

    public string? DescriptionRu { get; set; }
    public string? DescriptionEn { get; set; }

    [MaxLength(500)]
    public string? CoverUrl { get; set; }

    public int? TimeLimit { get; set; }

    public bool IsActive { get; set; } = true;
    public byte SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizResult> Results { get; set; } = new List<QuizResult>();
}

public enum QuizType
{
    ImageGuess,
    RegionGuess,
    DragDrop,
    CenturyGuess,
    OddOneOut
}
