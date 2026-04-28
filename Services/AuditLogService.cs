using System.Text.Json;
using BelarusHeritage.Data;
using BelarusHeritage.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace BelarusHeritage.Services;

public class AuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int? userId, string action, string entity, int? entityId, object? oldValues = null, object? newValues = null, string? ipAddress = null)
    {
        var payload = new Dictionary<string, object>();

        if (oldValues != null)
            payload["old"] = oldValues;

        if (newValues != null)
            payload["new"] = newValues;

        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Payload = payload.Any() ? JsonSerializer.Serialize(payload) : null,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 50)
    {
        return await Task.FromResult(_context.AuditLogs
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToList());
    }

    public async Task<List<AuditLog>> GetLogsAsync(
        int? userId = null,
        string? action = null,
        string? entity = null,
        int? entityId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.AuditLogs.Include(l => l.User).AsQueryable();

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);

        if (!string.IsNullOrWhiteSpace(entity))
            query = query.Where(l => l.Entity == entity);

        if (entityId.HasValue)
            query = query.Where(l => l.EntityId == entityId.Value);

        if (fromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.CreatedAt <= toDate.Value);

        return await Task.FromResult(query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList());
    }
}
