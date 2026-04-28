using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class TimelineEvent
{
    public int Id { get; set; }

    public int? ObjectId { get; set; }
    public HeritageObject? Object { get; set; }

    [Required]
    public short Year { get; set; }

    [Required]
    [MaxLength(300)]
    public string TitleRu { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string TitleBe { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string TitleEn { get; set; } = string.Empty;

    public string? BodyRu { get; set; }
    public string? BodyEn { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsPublished { get; set; } = true;
    public short SortOrder { get; set; } = 0;
}
