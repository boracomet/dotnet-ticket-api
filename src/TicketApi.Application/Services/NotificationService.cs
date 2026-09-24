using TicketApi.Application.DTOs;
using TicketApi.Application.Interfaces;

namespace TicketApi.Application.Services;

public class NotificationService
{
    private readonly INotificationRepository _notifications;

    public NotificationService(INotificationRepository notifications) => _notifications = notifications;

    public async Task<NotificationListResponse> ListAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _notifications.ListRecentAsync(userId, 20, ct);
        var unread = await _notifications.CountUnreadAsync(userId, ct);
        return new NotificationListResponse(
            unread,
            items.Select(n => new NotificationResponse(n.Id, n.TicketId, n.Message, n.IsRead, n.CreatedAtUtc)).ToList());
    }

    public Task MarkReadAsync(Guid userId, Guid id, CancellationToken ct = default) =>
        _notifications.MarkReadAsync(userId, id, ct);

    public Task MarkAllReadAsync(Guid userId, CancellationToken ct = default) =>
        _notifications.MarkAllReadAsync(userId, ct);
}
