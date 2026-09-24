using TicketApi.Domain.Entities;

namespace TicketApi.Application.Interfaces;

public interface INotificationRepository
{
    Task AddRangeAsync(IReadOnlyList<AppNotification> items, CancellationToken ct = default);
    Task<IReadOnlyList<AppNotification>> ListRecentAsync(Guid userId, int take, CancellationToken ct = default);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default);
    Task MarkReadAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}
