using TicketApi.Application.Common;
using TicketApi.Application.DTOs;
using TicketApi.Application.Interfaces;
using TicketApi.Domain.Entities;
using TicketApi.Domain.Enums;

namespace TicketApi.Application.Services;

public class TicketService
{
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new()
    {
        [TicketStatus.Open] = new[] { TicketStatus.InProgress, TicketStatus.Closed },
        [TicketStatus.InProgress] = new[] { TicketStatus.Resolved, TicketStatus.Open, TicketStatus.Closed },
        [TicketStatus.Resolved] = new[] { TicketStatus.Closed, TicketStatus.InProgress },
        [TicketStatus.Closed] = Array.Empty<TicketStatus>()
    };

    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly INotificationRepository _notifications;
    private readonly IEmailNotifier _email;
    private readonly IReplyFileStore _files;

    public TicketService(
        ITicketRepository tickets,
        IUserRepository users,
        INotificationRepository notifications,
        IEmailNotifier email,
        IReplyFileStore files)
    {
        _tickets = tickets;
        _users = users;
        _notifications = notifications;
        _email = email;
        _files = files;
    }

    public async Task<TicketResponse> CreateAsync(
        Guid userId, string role, CreateTicketRequest request,
        IReadOnlyList<ReplyFileUpload>? files = null, CancellationToken ct = default)
    {
        if (string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            throw new AppException(403, "FORBIDDEN", "Admins cannot create tickets");
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException(400, "VALIDATION_ERROR", "Title is required");

        var description = request.Description?.Trim() ?? string.Empty;
        if (description.Length > 4000)
            throw new AppException(400, "VALIDATION_ERROR", "Description is too long");

        var uploads = files ?? Array.Empty<ReplyFileUpload>();
        if (uploads.Count > ReplyFileRules.MaxFiles)
            throw new AppException(400, "VALIDATION_ERROR", "At most 3 files can be attached");

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = description,
            Priority = request.Priority,
            Status = TicketStatus.Open,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        foreach (var upload in uploads)
        {
            var (ext, contentType) = ReplyFileRules.Inspect(upload);
            var storedName = $"{ticket.Id:N}/{Guid.NewGuid():N}{ext}";
            await _files.SaveAsync(storedName, upload.Content, ct);
            var safeName = Path.GetFileName(upload.FileName);
            ticket.Attachments.Add(new TicketAttachment
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                FileName = safeName.Length > 260 ? safeName[..260] : safeName,
                StoredName = storedName,
                ContentType = contentType,
                SizeBytes = upload.Content.Length
            });
        }

        await _tickets.AddAsync(ticket, ct);
        return ToResponse(ticket);
    }

    public async Task<TicketResponse> GetAsync(Guid id, Guid callerId, string role, CancellationToken ct = default)
    {
        var ticket = await RequireTicket(id, ct);
        EnsureCanView(ticket, callerId, role);
        return ToResponse(ticket);
    }

    public async Task<PagedResult<TicketResponse>> ListAsync(
        TicketQuery query, Guid callerId, string role, CancellationToken ct = default)
    {
        if (query.Page < 1) query = query with { Page = 1 };
        if (query.PageSize < 1 || query.PageSize > 100) query = query with { PageSize = 10 };

        Guid? restrict = string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase)
            ? null
            : callerId;

        var page = await _tickets.QueryAsync(query, restrict, ct);
        return new PagedResult<TicketResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<TicketResponse> UpdateAsync(
        Guid id, Guid callerId, string role, UpdateTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await RequireTicket(id, ct);
        EnsureCanEdit(ticket, callerId, role);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException(400, "VALIDATION_ERROR", "Title is required");

        if (request.AssignedToUserId.HasValue)
        {
            _ = await _users.GetByIdAsync(request.AssignedToUserId.Value, ct)
                ?? throw new AppException(404, "USER_NOT_FOUND", "Assignee not found");
        }

        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description?.Trim() ?? string.Empty;
        ticket.Priority = request.Priority;
        ticket.AssignedToUserId = request.AssignedToUserId;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await _tickets.UpdateAsync(ticket, ct);
        return ToResponse(ticket);
    }

    public async Task<TicketResponse> ChangeStatusAsync(
        Guid id, Guid callerId, string role, ChangeStatusRequest request, CancellationToken ct = default)
    {
        var ticket = await RequireTicket(id, ct);
        if (!string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            throw new AppException(403, "FORBIDDEN", "Only an admin can change ticket status");

        if (ticket.Status == request.Status)
            return ToResponse(ticket);

        if (!AllowedTransitions.TryGetValue(ticket.Status, out var next)
            || !next.Contains(request.Status))
        {
            throw new AppException(422, "INVALID_STATUS_TRANSITION",
                $"Cannot transition from {ticket.Status} to {request.Status}");
        }

        ticket.Status = request.Status;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await _tickets.UpdateAsync(ticket, ct);
        return ToResponse(ticket);
    }

    public async Task<IReadOnlyList<TicketReplyResponse>> ListRepliesAsync(
        Guid id, Guid callerId, string role, CancellationToken ct = default)
    {
        var ticket = await RequireTicket(id, ct);
        EnsureCanView(ticket, callerId, role);
        var replies = await _tickets.ListRepliesAsync(id, ct);
        return replies.Select(ToReplyResponse).ToList();
    }

    public async Task<TicketReplyResponse> AddReplyAsync(
        Guid id, Guid callerId, string role, CreateTicketReplyRequest request,
        IReadOnlyList<ReplyFileUpload>? files = null, CancellationToken ct = default)
    {
        var ticket = await RequireTicket(id, ct);
        EnsureCanView(ticket, callerId, role);

        var body = request.Body?.Trim() ?? string.Empty;
        var uploads = files ?? Array.Empty<ReplyFileUpload>();
        if (body.Length == 0 && uploads.Count == 0)
            throw new AppException(400, "VALIDATION_ERROR", "Reply is required");
        if (body.Length > 4000)
            throw new AppException(400, "VALIDATION_ERROR", "Reply is too long");
        if (uploads.Count > ReplyFileRules.MaxFiles)
            throw new AppException(400, "VALIDATION_ERROR", "At most 3 files can be attached");

        var author = await _users.GetByIdAsync(callerId, ct)
            ?? throw new AppException(404, "USER_NOT_FOUND", "User not found");

        var reply = new TicketReply
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = callerId,
            Author = author,
            Body = body,
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var upload in uploads)
        {
            var (ext, contentType) = ReplyFileRules.Inspect(upload);
            var storedName = $"{reply.Id:N}/{Guid.NewGuid():N}{ext}";
            await _files.SaveAsync(storedName, upload.Content, ct);
            var safeName = Path.GetFileName(upload.FileName);
            reply.Attachments.Add(new ReplyAttachment
            {
                Id = Guid.NewGuid(),
                ReplyId = reply.Id,
                FileName = safeName.Length > 260 ? safeName[..260] : safeName,
                StoredName = storedName,
                ContentType = contentType,
                SizeBytes = upload.Content.Length
            });
        }

        await _tickets.AddReplyAsync(reply, ct);
        ticket.UpdatedAtUtc = reply.CreatedAtUtc;
        await _tickets.UpdateAsync(ticket, ct);
        await NotifyReplyAsync(ticket, author, role, ct);
        return ToReplyResponse(reply);
    }

    public async Task<AttachmentDownload> OpenTicketAttachmentAsync(
        Guid ticketId, Guid fileId, Guid callerId, string role, CancellationToken ct = default)
    {
        var ticket = await RequireTicket(ticketId, ct);
        EnsureCanView(ticket, callerId, role);
        var file = ticket.Attachments.FirstOrDefault(a => a.Id == fileId)
            ?? throw new AppException(404, "FILE_NOT_FOUND", "File not found");
        var bytes = await _files.ReadAsync(file.StoredName, ct)
            ?? throw new AppException(404, "FILE_NOT_FOUND", "File not found");
        return new AttachmentDownload(bytes, file.ContentType, file.FileName);
    }

    public async Task<AttachmentDownload> OpenAttachmentAsync(
        Guid ticketId, Guid replyId, Guid fileId, Guid callerId, string role, CancellationToken ct = default)
    {
        var ticket = await RequireTicket(ticketId, ct);
        EnsureCanView(ticket, callerId, role);
        var reply = await _tickets.GetReplyAsync(replyId, ct)
            ?? throw new AppException(404, "REPLY_NOT_FOUND", "Reply not found");
        if (reply.TicketId != ticketId)
            throw new AppException(404, "REPLY_NOT_FOUND", "Reply not found");

        var file = reply.Attachments.FirstOrDefault(a => a.Id == fileId)
            ?? throw new AppException(404, "FILE_NOT_FOUND", "File not found");
        var bytes = await _files.ReadAsync(file.StoredName, ct)
            ?? throw new AppException(404, "FILE_NOT_FOUND", "File not found");
        return new AttachmentDownload(bytes, file.ContentType, file.FileName);
    }

    public async Task DeleteAsync(Guid id, Guid callerId, string role, CancellationToken ct = default)
    {
        var ticket = await RequireTicket(id, ct);
        if (!string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            throw new AppException(403, "FORBIDDEN", "Only an admin can delete a ticket");

        await _tickets.DeleteAsync(ticket, ct);
    }

    private async Task<Ticket> RequireTicket(Guid id, CancellationToken ct) =>
        await _tickets.GetByIdAsync(id, ct)
        ?? throw new AppException(404, "TICKET_NOT_FOUND", "Ticket not found");

    private static void EnsureCanView(Ticket ticket, Guid callerId, string role)
    {
        if (string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase)) return;
        if (ticket.CreatedByUserId == callerId || ticket.AssignedToUserId == callerId) return;
        throw new AppException(403, "FORBIDDEN", "Not allowed to view this ticket");
    }

    private static void EnsureCanEdit(Ticket ticket, Guid callerId, string role)
    {
        if (string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase)) return;
        if (ticket.CreatedByUserId == callerId || ticket.AssignedToUserId == callerId) return;
        throw new AppException(403, "FORBIDDEN", "Not allowed to modify this ticket");
    }

    private static TicketResponse ToResponse(Ticket t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority,
        t.CreatedByUserId, t.AssignedToUserId, t.CreatedAtUtc, t.UpdatedAtUtc,
        (t.Attachments ?? new List<TicketAttachment>())
            .OrderBy(a => a.FileName)
            .Select(a => new ReplyAttachmentResponse(a.Id, a.FileName, a.ContentType, a.SizeBytes))
            .ToList());

    private async Task NotifyReplyAsync(Ticket ticket, AppUser author, string role, CancellationToken ct)
    {
        var isAdmin = string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        var recipients = new List<AppUser>();
        if (isAdmin)
        {
            if (ticket.CreatedByUserId != author.Id)
            {
                var owner = await _users.GetByIdAsync(ticket.CreatedByUserId, ct);
                if (owner != null) recipients.Add(owner);
            }

            if (ticket.AssignedToUserId is Guid assigneeId
                && assigneeId != author.Id
                && recipients.All(u => u.Id != assigneeId))
            {
                var assignee = await _users.GetByIdAsync(assigneeId, ct);
                if (assignee != null) recipients.Add(assignee);
            }
        }
        else
        {
            var admins = await _users.ListByRoleAsync(Roles.Admin, ct);
            recipients.AddRange(admins.Where(a => a.Id != author.Id));
        }

        if (recipients.Count == 0) return;

        var message = isAdmin ? "Admin cevap verdi" : $"{author.FullName} cevap verdi";
        var now = DateTime.UtcNow;
        var notes = recipients.Select(user => new AppNotification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = user.Id,
            TicketId = ticket.Id,
            Message = message,
            CreatedAtUtc = now
        }).ToList();
        await _notifications.AddRangeAsync(notes, ct);
        await _email.NotifyAsync(
            recipients.Select(u => new EmailRecipient(u.Email, u.FullName)).ToList(),
            message,
            $"{message}\n\nTalep: {ticket.Title}",
            ct);
    }

    private static TicketReplyResponse ToReplyResponse(TicketReply reply) => new(
        reply.Id,
        reply.TicketId,
        reply.AuthorUserId,
        reply.Author?.FullName ?? "Kullanıcı",
        reply.Body,
        reply.CreatedAtUtc,
        (reply.Attachments ?? new List<ReplyAttachment>())
            .OrderBy(a => a.FileName)
            .Select(a => new ReplyAttachmentResponse(a.Id, a.FileName, a.ContentType, a.SizeBytes))
            .ToList());
}
