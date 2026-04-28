using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using Microsoft.EntityFrameworkCore;
using HeritageRoute = BelarusHeritage.Models.Domain.Route;

namespace BelarusHeritage.Services;

public class RouteService
{
    private readonly AppDbContext _context;

    public RouteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<HeritageRoute>> GetUserRoutesAsync(int userId)
    {
        return await _context.Routes
            .Include(r => r.Stops).ThenInclude(s => s.Object).ThenInclude(o => o!.Category)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();
    }

    public async Task<HeritageRoute?> GetRouteAsync(int routeId, int userId)
    {
        return await _context.Routes
            .Include(r => r.Stops.OrderBy(s => s.SortOrder))
            .ThenInclude(s => s.Object)
            .ThenInclude(o => o!.Category)
            .Include(r => r.Stops)
            .ThenInclude(s => s.Object)
            .ThenInclude(o => o!.Location)
            .FirstOrDefaultAsync(r => r.Id == routeId && r.UserId == userId);
    }

    public async Task<HeritageRoute?> GetPublicRouteByTokenAsync(string shareToken)
    {
        return await _context.Routes
            .Include(r => r.User)
            .Include(r => r.Stops.OrderBy(s => s.SortOrder))
            .ThenInclude(s => s.Object)
            .ThenInclude(o => o!.Category)
            .Include(r => r.Stops)
            .ThenInclude(s => s.Object)
            .ThenInclude(o => o!.Location)
            .Include(r => r.Stops)
            .ThenInclude(s => s.Object)
            .ThenInclude(o => o!.Images.Where(i => i.IsMain))
            .FirstOrDefaultAsync(r => r.ShareToken == shareToken && r.IsPublic);
    }

    public async Task<HeritageRoute> CreateRouteAsync(HeritageRoute route)
    {
        route.CreatedAt = DateTime.UtcNow;
        route.UpdatedAt = DateTime.UtcNow;
        route.ShareToken = GenerateShareToken();

        _context.Routes.Add(route);
        await _context.SaveChangesAsync();

        // Calculate total distance
        await RecalculateRouteDistance(route.Id);

        return route;
    }

    public async Task<HeritageRoute> UpdateRouteAsync(HeritageRoute route)
    {
        if (route.IsPublic)
        {
            route.StartAddress = null;
            route.StartLat = null;
            route.StartLng = null;
            route.EndAddress = null;
            route.EndLat = null;
            route.EndLng = null;
        }

        route.UpdatedAt = DateTime.UtcNow;
        _context.Routes.Update(route);
        await _context.SaveChangesAsync();

        await RecalculateRouteDistance(route.Id);

        return route;
    }

    public async Task AddStopAsync(int routeId, int objectId, string? notes = null)
    {
        var route = await _context.Routes.FindAsync(routeId);
        if (route == null) return;

        var exists = await _context.RouteStops.AnyAsync(s => s.RouteId == routeId && s.ObjectId == objectId);
        if (exists) return;

        var maxOrder = await _context.RouteStops
            .Where(s => s.RouteId == routeId)
            .MaxAsync(s => (byte?)s.SortOrder) ?? 0;

        _context.RouteStops.Add(new RouteStop
        {
            RouteId = routeId,
            ObjectId = objectId,
            SortOrder = (byte)(maxOrder + 1),
            Notes = notes
        });

        await _context.SaveChangesAsync();
        await RecalculateRouteDistance(routeId);
    }

    public async Task RemoveStopAsync(int routeId, int objectId)
    {
        var stop = await _context.RouteStops
            .FirstOrDefaultAsync(s => s.RouteId == routeId && s.ObjectId == objectId);

        if (stop != null)
        {
            _context.RouteStops.Remove(stop);
            await _context.SaveChangesAsync();
            await RecalculateRouteDistance(routeId);
        }
    }

    public async Task ReorderStopsAsync(int routeId, List<int> objectIds)
    {
        var stops = await _context.RouteStops
            .Where(s => s.RouteId == routeId)
            .ToListAsync();

        for (int i = 0; i < objectIds.Count; i++)
        {
            var stop = stops.FirstOrDefault(s => s.ObjectId == objectIds[i]);
            if (stop != null)
                stop.SortOrder = (byte)i;
        }

        await _context.SaveChangesAsync();
        await RecalculateRouteDistance(routeId);
    }

    public async Task<bool> OptimizeStopsAsync(int routeId, int userId)
    {
        var route = await _context.Routes
            .Include(r => r.Stops)
            .ThenInclude(s => s.Object)
            .ThenInclude(o => o!.Location)
            .FirstOrDefaultAsync(r => r.Id == routeId && r.UserId == userId);

        if (route == null || route.Stops.Count < 2)
            return false;

        var orderedOriginal = route.Stops.OrderBy(s => s.SortOrder).ToList();
        var withCoords = orderedOriginal
            .Where(s => s.Object?.Location != null)
            .ToList();

        if (withCoords.Count < 2)
            return false;

        var noCoords = orderedOriginal
            .Where(s => s.Object?.Location == null)
            .ToList();

        (double lat, double lng)? startPoint = null;
        if (route.StartLat.HasValue && route.StartLng.HasValue)
            startPoint = ((double)route.StartLat.Value, (double)route.StartLng.Value);

        (double lat, double lng)? endPoint = null;
        if (route.EndLat.HasValue && route.EndLng.HasValue)
            endPoint = ((double)route.EndLat.Value, (double)route.EndLng.Value);

        var unvisited = new List<RouteStop>(withCoords);
        var optimized = new List<RouteStop>(withCoords.Count);

        (double lat, double lng) current;
        if (startPoint.HasValue)
        {
            current = startPoint.Value;
        }
        else
        {
            var first = orderedOriginal.First(s => s.Object?.Location != null);
            optimized.Add(first);
            unvisited.Remove(first);
            var firstLoc = first.Object!.Location!;
            current = ((double)firstLoc.Lat, (double)firstLoc.Lng);
        }

        while (unvisited.Count > 0)
        {
            RouteStop? best = null;
            double bestScore = double.MaxValue;

            foreach (var candidate in unvisited)
            {
                var loc = candidate.Object!.Location!;
                var toCandidate = CalculateHaversineDistance(current.lat, current.lng, (double)loc.Lat, (double)loc.Lng);
                var score = toCandidate;
                if (endPoint.HasValue)
                {
                    // Small bias toward moving in the end-point direction.
                    score += CalculateHaversineDistance((double)loc.Lat, (double)loc.Lng, endPoint.Value.lat, endPoint.Value.lng) * 0.15;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best == null) break;

            optimized.Add(best);
            unvisited.Remove(best);
            var bestLoc = best.Object!.Location!;
            current = ((double)bestLoc.Lat, (double)bestLoc.Lng);
        }

        var finalOrdered = optimized.Concat(noCoords).ToList();
        for (int i = 0; i < finalOrdered.Count; i++)
        {
            finalOrdered[i].SortOrder = (byte)i;
        }

        await _context.SaveChangesAsync();
        await RecalculateRouteDistance(routeId);
        return true;
    }

    public async Task DeleteRouteAsync(int routeId, int userId)
    {
        var route = await _context.Routes
            .FirstOrDefaultAsync(r => r.Id == routeId && r.UserId == userId);

        if (route != null)
        {
            _context.Routes.Remove(route);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<HeritageRoute> CopyRouteAsync(int routeId, int userId)
    {
        var original = await GetRouteAsync(routeId, userId);
        if (original == null)
            throw new ArgumentException("Route not found");

        var copy = new HeritageRoute
        {
            UserId = userId,
            Title = $"Копия: {original.Title}",
            Description = original.Description,
            IsPublic = false,
            ShareToken = GenerateShareToken(),
            StartAddress = original.StartAddress,
            StartLat = original.StartLat,
            StartLng = original.StartLng,
            EndAddress = original.EndAddress,
            EndLat = original.EndLat,
            EndLng = original.EndLng
        };

        _context.Routes.Add(copy);
        await _context.SaveChangesAsync();

        foreach (var stop in original.Stops)
        {
            _context.RouteStops.Add(new RouteStop
            {
                RouteId = copy.Id,
                ObjectId = stop.ObjectId,
                SortOrder = stop.SortOrder,
                Notes = stop.Notes
            });
        }

        await _context.SaveChangesAsync();

        return copy;
    }

    public async Task<List<HeritageRoute>> GetPublicRoutesAsync(int count = 20)
    {
        return await _context.Routes
            .Include(r => r.User)
            .Include(r => r.Stops).ThenInclude(s => s.Object).ThenInclude(o => o!.Images.Where(i => i.IsMain))
            .Where(r => r.IsPublic)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<HeritageRoute?> SavePersonalCopyFromPublicAsync(
        string shareToken,
        int userId,
        string? title,
        string? startAddress,
        decimal? startLat,
        decimal? startLng,
        string? endAddress,
        decimal? endLat,
        decimal? endLng,
        List<int>? orderedObjectIds)
    {
        var source = await _context.Routes
            .Include(r => r.Stops.OrderBy(s => s.SortOrder))
            .ThenInclude(s => s.Object)
            .ThenInclude(o => o!.Location)
            .FirstOrDefaultAsync(r => r.ShareToken == shareToken && r.IsPublic);

        if (source == null)
            return null;

        var sourceStops = source.Stops.OrderBy(s => s.SortOrder).ToList();
        var validObjectIds = sourceStops.Select(s => s.ObjectId).ToHashSet();

        var finalOrder = new List<int>();
        if (orderedObjectIds != null && orderedObjectIds.Count > 0)
        {
            foreach (var objectId in orderedObjectIds)
            {
                if (validObjectIds.Contains(objectId) && !finalOrder.Contains(objectId))
                    finalOrder.Add(objectId);
            }
        }

        foreach (var s in sourceStops)
        {
            if (!finalOrder.Contains(s.ObjectId))
                finalOrder.Add(s.ObjectId);
        }

        var copy = new HeritageRoute
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? $"Мой маршрут: {source.Title}" : title.Trim(),
            Description = source.Description,
            IsPublic = false,
            ShareToken = GenerateShareToken(),
            StartAddress = string.IsNullOrWhiteSpace(startAddress) ? null : startAddress.Trim(),
            StartLat = startLat,
            StartLng = startLng,
            EndAddress = string.IsNullOrWhiteSpace(endAddress) ? null : endAddress.Trim(),
            EndLat = endLat,
            EndLng = endLng,
            SourceRouteId = source.Id,
            SourceRouteTitle = source.Title,
            SourceRouteShareToken = source.ShareToken
        };

        _context.Routes.Add(copy);
        await _context.SaveChangesAsync();

        for (int i = 0; i < finalOrder.Count; i++)
        {
            _context.RouteStops.Add(new RouteStop
            {
                RouteId = copy.Id,
                ObjectId = finalOrder[i],
                SortOrder = (byte)i
            });
        }

        await _context.SaveChangesAsync();
        await RecalculateRouteDistance(copy.Id);
        return copy;
    }

    private async Task RecalculateRouteDistance(int routeId)
    {
        var route = await _context.Routes
            .Include(r => r.Stops).ThenInclude(s => s.Object).ThenInclude(o => o!.Location)
            .FirstOrDefaultAsync(r => r.Id == routeId);

        if (route == null)
        {
            return;
        }

        var points = new List<(double lat, double lng)>();
        if (route.StartLat.HasValue && route.StartLng.HasValue)
        {
            points.Add(((double)route.StartLat.Value, (double)route.StartLng.Value));
        }

        var orderedStops = route.Stops.OrderBy(s => s.SortOrder).ToList();
        foreach (var stop in orderedStops)
        {
            var loc = stop.Object?.Location;
            if (loc != null)
            {
                points.Add(((double)loc.Lat, (double)loc.Lng));
            }
        }

        if (route.EndLat.HasValue && route.EndLng.HasValue)
        {
            points.Add(((double)route.EndLat.Value, (double)route.EndLng.Value));
        }

        if (points.Count < 2)
        {
            route.TotalKm = null;
            await _context.SaveChangesAsync();
            return;
        }

        double totalKm = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            var from = points[i];
            var to = points[i + 1];
            totalKm += CalculateHaversineDistance(from.lat, from.lng, to.lat, to.lng);
        }

        route.TotalKm = (decimal)Math.Round(totalKm, 1);
        route.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static double CalculateHaversineDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadius = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static string GenerateShareToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_")
            .Replace("+", "-")
            .Substring(0, 22);
    }
}
