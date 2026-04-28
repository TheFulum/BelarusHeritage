using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BelarusHeritage.Models.Domain;

public class ObjectImage
{
    public int Id { get; set; }

    [Required]
    public int ObjectId { get; set; }
    public HeritageObject? Object { get; set; }

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ThumbUrl { get; set; }

    [MaxLength(300)]
    public string? CaptionRu { get; set; }
    [MaxLength(300)]
    public string? CaptionBe { get; set; }
    [MaxLength(300)]
    public string? CaptionEn { get; set; }

    public bool IsMain { get; set; } = false;
    public bool Is360 { get; set; } = false;
    public byte SortOrder { get; set; } = 0;

    public int? UploadedBy { get; set; }
    public User? Uploader { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
