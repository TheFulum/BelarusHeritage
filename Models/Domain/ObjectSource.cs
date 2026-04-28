using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class ObjectSource
{
    public int Id { get; set; }

    [Required]
    public int ObjectId { get; set; }
    public HeritageObject? Object { get; set; }

    public SourceType Type { get; set; } = SourceType.Other;

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Author { get; set; }

    [MaxLength(300)]
    public string? Publisher { get; set; }

    public int? Year { get; set; }

    [MaxLength(1000)]
    public string? Url { get; set; }

    [MaxLength(50)]
    public string? Pages { get; set; }

    public byte SortOrder { get; set; } = 0;
}

public enum SourceType
{
    Book,
    Article,
    Website,
    Archive,
    Museum,
    Other
}
