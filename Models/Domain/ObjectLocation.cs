using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BelarusHeritage.Models.Domain;

public class ObjectLocation
{
    public int Id { get; set; }

    [Required]
    public int ObjectId { get; set; }
    public HeritageObject? Object { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal Lat { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal Lng { get; set; }

    [MaxLength(300)]
    public string? AddressRu { get; set; }
    [MaxLength(300)]
    public string? AddressBe { get; set; }
    [MaxLength(300)]
    public string? AddressEn { get; set; }

    public byte MapZoom { get; set; } = 15;
}
