using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface IAuditService
{
    Task LogAsync(string userId, string action, string description);
    Task<AuditPageViewModel> GetPageAsync(string? type, string? search, int page);
}

public class AuditService : IAuditService
{
    private const int PageSize = 25;

    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Записывает действие в журнал
    public async Task LogAsync(string userId, string action, string description)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            Description = description.Length > 1000 ? description[..1000] : description,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task<AuditPageViewModel> GetPageAsync(string? type, string? search, int page)
    {
        var query = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(a => a.Action == type);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(a => EF.Functions.ILike(a.Description ?? "", pattern)
                                     || EF.Functions.ILike(a.User.FullName, pattern));
        }

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var rows = await query
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(a => new AuditRowViewModel
            {
                CreatedAt = a.CreatedAt,
                UserName = a.User.FullName,
                Action = a.Action,
                Description = a.Description
            })
            .ToListAsync();

        foreach (var row in rows)
            row.ActionLabel = AuditActions.GetLabel(row.Action);

        // Для фильтра берём только те виды действий, которые уже есть в журнале
        var types = (await _db.AuditLogs.AsNoTracking().Select(a => a.Action).Distinct().ToListAsync())
            .Select(a => new AuditActionOptionViewModel { Value = a, Label = AuditActions.GetLabel(a) })
            .OrderBy(o => o.Label)
            .ToList();

        return new AuditPageViewModel
        {
            Type = type,
            Search = search,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Items = rows,
            Types = types
        };
    }
}