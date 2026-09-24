namespace TicketApi.Domain.Entities;

public class AppNotification
{
    public Guid Id { get; set; }
    public Guid RecipientUserId { get; set; }
    public AppUser? Recipient { get; set; }
    public Guid TicketId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
