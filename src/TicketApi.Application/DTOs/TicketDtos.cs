using TicketApi.Domain.Enums;

namespace TicketApi.Application.DTOs;

public record TicketResponse(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid CreatedByUserId,
    Guid? AssignedToUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<ReplyAttachmentResponse> Attachments);

public record CreateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority);

public record UpdateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority,
    Guid? AssignedToUserId);

public record ChangeStatusRequest(TicketStatus Status);

public record ReplyAttachmentResponse(Guid Id, string FileName, string ContentType, long SizeBytes);

public record TicketReplyResponse(
    Guid Id,
    Guid TicketId,
    Guid AuthorUserId,
    string AuthorName,
    string Body,
    DateTime CreatedAtUtc,
    IReadOnlyList<ReplyAttachmentResponse> Attachments);

public record CreateTicketReplyRequest(string? Body);

public record ReplyFileUpload(string FileName, string ContentType, byte[] Content);

public record AttachmentDownload(byte[] Content, string ContentType, string FileName);

public record NotificationResponse(Guid Id, Guid TicketId, string Message, bool IsRead, DateTime CreatedAtUtc);

public record NotificationListResponse(int UnreadCount, IReadOnlyList<NotificationResponse> Items);

public record SmtpSettingsResponse(
    string Host,
    int Port,
    string Username,
    bool HasPassword,
    string FromEmail,
    string FromName,
    bool EnableSsl,
    bool Enabled);

public record UpdateSmtpSettingsRequest(
    string Host,
    int Port,
    string? Username,
    string? Password,
    string FromEmail,
    string FromName,
    bool EnableSsl,
    bool Enabled);

public record SmtpTestRequest(string ToEmail);

public record TicketQuery(
    int Page = 1,
    int PageSize = 10,
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    string? Search = null);
