using TicketApi.Domain.Enums;

namespace TicketApi.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public Guid CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<TicketReply> Replies { get; set; } = new List<TicketReply>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}
