using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class QuizResult
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public byte Score { get; set; }
    public byte CorrectCount { get; set; }
    public byte TotalCount { get; set; }
    public int? TimeSpentSec { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
