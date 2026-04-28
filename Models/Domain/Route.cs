using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class Route
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; } = false;

    [MaxLength(32)]
    public string? ShareToken { get; set; }

    public decimal? TotalKm { get; set; }

    [MaxLength(255)]
    public string? StartAddress { get; set; }
    public decimal? StartLat { get; set; }
    public decimal? StartLng { get; set; }

    [MaxLength(255)]
    public string? EndAddress { get; set; }
    public decimal? EndLat { get; set; }
    public decimal? EndLng { get; set; }

    public int? SourceRouteId { get; set; }
    [MaxLength(255)]
    public string? SourceRouteTitle { get; set; }
    [MaxLength(32)]
    public string? SourceRouteShareToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
}
