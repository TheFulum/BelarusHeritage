using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Services;

public class RatingService
{
    private readonly AppDbContext _context;

    public RatingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Rating?> GetUserRatingAsync(int objectId, int userId)
    {
        return await _context.Ratings
            .FirstOrDefaultAsync(r => r.ObjectId == objectId && r.UserId == userId);
    }

    public async Task SetRatingAsync(int objectId, int userId, byte value)
    {
        if (value < 1 || value > 5)
            throw new ArgumentException("Rating must be between 1 and 5");

        var existing = await _context.Ratings
            .FirstOrDefaultAsync(r => r.ObjectId == objectId && r.UserId == userId);

        var obj = await _context.HeritageObjects.FindAsync(objectId);
        if (obj == null)
            throw new ArgumentException("Object not found");

        if (existing != null)
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.Ratings.Add(new Rating
            {
                ObjectId = objectId,
                UserId = userId,
                Value = value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Recalculate average
        var ratings = await _context.Ratings
            .Where(r => r.ObjectId == objectId)
            .ToListAsync();

        obj.RatingCount = ratings.Count;
        obj.RatingAvg = ratings.Any() ? (decimal)ratings.Average(r => r.Value) : 0;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteRatingAsync(int objectId, int userId)
    {
        var rating = await _context.Ratings
            .FirstOrDefaultAsync(r => r.ObjectId == objectId && r.UserId == userId);

        if (rating != null)
        {
            _context.Ratings.Remove(rating);

            var obj = await _context.HeritageObjects.FindAsync(objectId);
            if (obj != null)
            {
                var remainingRatings = await _context.Ratings
                    .Where(r => r.ObjectId == objectId)
                    .ToListAsync();

                obj.RatingCount = remainingRatings.Count;
                obj.RatingAvg = remainingRatings.Any() ? (decimal)remainingRatings.Average(r => r.Value) : 0;
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Rating>> GetUserRatingsAsync(int userId)
    {
        return await _context.Ratings
            .Include(r => r.Object).ThenInclude(o => o!.Region)
            .Include(r => r.Object).ThenInclude(o => o!.Images.Where(i => i.IsMain))
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();
    }
}
