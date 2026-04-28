using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class UserToken
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    public TokenType Type { get; set; }

    [Required]
    [MaxLength(255)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TokenType
{
    EmailVerify,
    PasswordReset,
    Refresh
}
