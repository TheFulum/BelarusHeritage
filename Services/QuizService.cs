using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Services;

public class QuizService
{
    private readonly AppDbContext _context;

    public QuizService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Quiz>> GetActiveQuizzesAsync()
    {
        return await _context.Quizzes
            .Where(q => q.IsActive)
            .Include(q => q.Questions)
            .OrderBy(q => q.SortOrder)
            .ToListAsync();
    }

    public async Task<Quiz?> GetQuizBySlugAsync(string slug)
    {
        var requestedSlug = NormalizeSlug(slug);

        var quizzes = await _context.Quizzes
            .Include(q => q.Questions.OrderBy(q => q.SortOrder))
            .ThenInclude(q => q.Answers.OrderBy(a => a.SortOrder))
            .Where(q => q.IsActive)
            .ToListAsync();

        var directMatch = quizzes.FirstOrDefault(q => NormalizeSlug(q.Slug) == requestedSlug);
        if (directMatch != null)
            return directMatch;

        // Backward compatibility: old/alternative slugs can still resolve.
        if (requestedSlug == "ugadai-region")
        {
            return quizzes.FirstOrDefault(q => q.Type == QuizType.RegionGuess);
        }

        return null;
    }

    public async Task<QuizResult> SaveResultAsync(int? userId, int quizId, byte score, byte correctCount, byte totalCount, int? timeSpentSec = null)
    {
        var result = new QuizResult
        {
            UserId = userId,
            QuizId = quizId,
            Score = score,
            CorrectCount = correctCount,
            TotalCount = totalCount,
            TimeSpentSec = timeSpentSec,
            CompletedAt = DateTime.UtcNow
        };

        _context.QuizResults.Add(result);
        await _context.SaveChangesAsync();

        return result;
    }

    public async Task<List<QuizResult>> GetUserResultsAsync(int userId)
    {
        return await _context.QuizResults
            .Include(r => r.Quiz)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CompletedAt)
            .ToListAsync();
    }

    public async Task<Quiz?> GetQuizWithStatsAsync(string slug)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions.OrderBy(q => q.SortOrder))
            .ThenInclude(q => q.Answers.OrderBy(a => a.SortOrder))
            .FirstOrDefaultAsync(q => q.Slug == slug);

        if (quiz != null)
        {
            var results = await _context.QuizResults
                .Where(r => r.QuizId == quiz.Id)
                .ToListAsync();

            quiz.Questions = quiz.Questions.Select(q =>
            {
                // Remove correct answer flag from question
                q.Answers = q.Answers.Select(a =>
                {
                    a.IsCorrect = false;
                    return a;
                }).ToList();
                return q;
            }).ToList();
        }

        return quiz;
    }

    public async Task<QuizStatistics> GetQuizStatisticsAsync(int quizId)
    {
        var results = await _context.QuizResults
            .Where(r => r.QuizId == quizId)
            .ToListAsync();

        return new QuizStatistics
        {
            TotalAttempts = results.Count,
            AverageScore = results.Any() ? results.Average(r => r.Score) : 0,
            BestScore = results.Any() ? results.Max(r => r.Score) : (byte)0,
            AverageTimeSec = results.Where(r => r.TimeSpentSec.HasValue).Select(r => (double)r.TimeSpentSec!.Value).DefaultIfEmpty(0).Average()
        };
    }

    private static string NormalizeSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return string.Empty;

        return slug.Trim()
            .ToLowerInvariant()
            .Replace("_", "-")
            .Replace(" ", "-");
    }
}

public class QuizStatistics
{
    public int TotalAttempts { get; set; }
    public double AverageScore { get; set; }
    public byte BestScore { get; set; }
    public double AverageTimeSec { get; set; }
}
