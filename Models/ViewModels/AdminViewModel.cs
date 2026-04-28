using BelarusHeritage.Models.Domain;

namespace BelarusHeritage.Models.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalObjects { get; set; }
    public int PublishedObjects { get; set; }
    public int DraftObjects { get; set; }
    public int DeletedObjects { get; set; }
    public int TotalUsers { get; set; }
    public int NewUsersLast30Days { get; set; }
    public int PendingComments { get; set; }
    public int TimelineEvents { get; set; }

    public Dictionary<string, int> ObjectsByRegion { get; set; } = new();
    public Dictionary<string, int> ObjectsByCategory { get; set; } = new();
    public List<UserRegistrationData> UserRegistrations { get; set; } = new();
    public List<AuditLog> RecentLogs { get; set; } = new();
}

public class UserRegistrationData
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class AdminObjectsViewModel
{
    public List<HeritageObject> Objects { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
    public string? Status { get; set; }
    public int? RegionId { get; set; }
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
}

public class AdminTrashViewModel
{
    public List<HeritageObject> Objects { get; set; } = new();
    public int TotalCount { get; set; }
}

public class AdminUsersViewModel
{
    public List<User> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public string? Role { get; set; }
    public string? Search { get; set; }
}

public class AdminCommentsViewModel
{
    public List<Comment> Comments { get; set; } = new();
    public int TotalCount { get; set; }
    public string? Status { get; set; }
    public int? ObjectId { get; set; }
    public int? UserId { get; set; }
}

public class AdminQuizzesViewModel
{
    public List<Quiz> Quizzes { get; set; } = new();
    public Dictionary<int, QuizStatistics> Statistics { get; set; } = new();
}

public class QuizStatistics
{
    public int TotalAttempts { get; set; }
    public int UniqueUsers { get; set; }
    public double AverageScore { get; set; }
    public int HighScore { get; set; }
    public int LowScore { get; set; }

    public static implicit operator QuizStatistics(Services.QuizStatistics v) => new()
    {
        TotalAttempts = v.TotalAttempts,
        AverageScore  = v.AverageScore,
        HighScore     = v.BestScore,
        UniqueUsers   = 0,
        LowScore      = 0
    };
}
