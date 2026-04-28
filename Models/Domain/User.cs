using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BelarusHeritage.Models.Domain;

public class User : IdentityUser<int>
{
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(2)]
    public string PreferredLang { get; set; } = "ru";

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? GoogleId { get; set; }

    public string Role { get; set; } = "user";

    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<Route> Routes { get; set; } = new List<Route>();
    public ICollection<Ornament> Ornaments { get; set; } = new List<Ornament>();
    public ICollection<QuizResult> QuizResults { get; set; } = new List<QuizResult>();
    public ICollection<UserToken> Tokens { get; set; } = new List<UserToken>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class UserRole : IdentityRole<int>
{
}
