using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class Comment
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public int ObjectId { get; set; }
    public HeritageObject? Object { get; set; }

    [Required]
    public string Body { get; set; } = string.Empty;

    public CommentStatus Status { get; set; } = CommentStatus.Pending;

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum CommentStatus
{
    Pending,
    Approved,
    Rejected,
    Spam
}
