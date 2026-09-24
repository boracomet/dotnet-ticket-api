using Microsoft.EntityFrameworkCore;
using TicketApi.Application.Interfaces;
using TicketApi.Domain.Entities;
using TicketApi.Infrastructure.Persistence;

namespace TicketApi.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db) => _db = db;

    public async Task AddRangeAsync(IReadOnlyList<AppNotification> items, CancellationToken ct = default)
    {
        if (items.Count == 0) return;
        _db.Notifications.AddRange(items);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AppNotification>> ListRecentAsync(Guid userId, int take, CancellationToken ct = default) =>
        await _db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default) =>
        _db.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead, ct);

    public async Task MarkReadAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var item = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId, ct);
        if (item == null || item.IsRead) return;
        item.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _db.Notifications.Where(n => n.RecipientUserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var item in items) item.IsRead = true;
        if (items.Count > 0) await _db.SaveChangesAsync(ct);
    }
}
