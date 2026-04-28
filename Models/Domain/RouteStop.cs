using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class RouteStop
{
    public int Id { get; set; }

    [Required]
    public int RouteId { get; set; }
    public Route? Route { get; set; }

    [Required]
    public int ObjectId { get; set; }
    public HeritageObject? Object { get; set; }

    public byte SortOrder { get; set; } = 0;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
