using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BelarusHeritage.Models.Domain;

public class HeritageObject
{
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string NameRu { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string NameBe { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string NameEn { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required]
    public int RegionId { get; set; }
    public Region? Region { get; set; }

    public int? ArchStyleId { get; set; }
    public ArchStyle? ArchStyle { get; set; }

    public short? CenturyStart { get; set; }
    public short? CenturyEnd { get; set; }
    public int? BuildYear { get; set; }

    public string? DescriptionRu { get; set; }
    public string? DescriptionBe { get; set; }
    public string? DescriptionEn { get; set; }

    [MaxLength(300)]
    public string? ShortDescRu { get; set; }
    [MaxLength(300)]
    public string? ShortDescBe { get; set; }
    [MaxLength(300)]
    public string? ShortDescEn { get; set; }

    [MaxLength(500)]
    public string? FunFactRu { get; set; }
    [MaxLength(500)]
    public string? FunFactBe { get; set; }
    [MaxLength(500)]
    public string? FunFactEn { get; set; }

    [MaxLength(255)]
    public string? Architect { get; set; }

    public int? HeritageCategory { get; set; }
    public int? HeritageYear { get; set; }

    public PreservationStatus PreservationStatus { get; set; } = PreservationStatus.Preserved;

    public bool IsVisitable { get; set; } = true;
    [MaxLength(255)]
    public string? VisitingHours { get; set; }
    [MaxLength(100)]
    public string? EntryFee { get; set; }

    [MaxLength(500)]
    public string? MainImageUrl { get; set; }

    public ObjectStatus Status { get; set; } = ObjectStatus.Draft;
    public bool IsDeleted { get; set; } = false;
    public bool IsFeatured { get; set; } = false;

    [Column(TypeName = "decimal(3,2)")]
    public decimal? RatingAvg { get; set; }
    public int RatingCount { get; set; } = 0;

    public int? CreatedBy { get; set; }
    public User? Creator { get; set; }
    public int? UpdatedBy { get; set; }
    public User? Updater { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ObjectImage> Images { get; set; } = new List<ObjectImage>();
    public ObjectLocation? Location { get; set; }
    public ICollection<ObjectTagMap> TagMaps { get; set; } = new List<ObjectTagMap>();
    public ICollection<ObjectRelation> RelatedFrom { get; set; } = new List<ObjectRelation>();
    public ICollection<ObjectRelation> RelatedTo { get; set; } = new List<ObjectRelation>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<ObjectSource> Sources { get; set; } = new List<ObjectSource>();
    public ICollection<TimelineEvent> TimelineEvents { get; set; } = new List<TimelineEvent>();
}

public enum PreservationStatus
{
    Preserved,
    Partial,
    Ruins,
    Lost
}

public enum ObjectStatus
{
    Draft,
    Published,
    Archived
}
