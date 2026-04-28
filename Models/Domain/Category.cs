using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(60)]
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

    [MaxLength(60)]
    public string? IconClass { get; set; }

    [MaxLength(7)]
    public string? ColorHex { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<HeritageObject> Objects { get; set; } = new List<HeritageObject>();
}
