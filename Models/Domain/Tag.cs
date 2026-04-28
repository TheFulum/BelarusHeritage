using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class Tag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NameRu { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NameBe { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NameEn { get; set; } = string.Empty;

    [MaxLength(7)]
    public string? ColorHex { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ObjectTagMap> ObjectTagMaps { get; set; } = new List<ObjectTagMap>();
}
