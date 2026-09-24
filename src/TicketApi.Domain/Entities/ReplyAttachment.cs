namespace TicketApi.Domain.Entities;

public class ReplyAttachment
{
    public Guid Id { get; set; }
    public Guid ReplyId { get; set; }
    public TicketReply? Reply { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
