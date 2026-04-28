using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class Region
{
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NameRu { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NameBe { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NameEn { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<HeritageObject> Objects { get; set; } = new List<HeritageObject>();
}
