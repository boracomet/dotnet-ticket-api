namespace TicketApi.Domain.Entities;

public class TicketReply
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public Guid AuthorUserId { get; set; }
    public AppUser? Author { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ReplyAttachment> Attachments { get; set; } = new List<ReplyAttachment>();
}
