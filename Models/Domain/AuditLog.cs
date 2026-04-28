using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BelarusHeritage.Models.Domain;

public class AuditLog
{
    public long Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string Entity { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    [Column(TypeName = "json")]
    public string? Payload { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
