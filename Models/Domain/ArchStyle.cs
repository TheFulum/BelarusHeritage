using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class ArchStyle
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

    public ICollection<HeritageObject> Objects { get; set; } = new List<HeritageObject>();
}
