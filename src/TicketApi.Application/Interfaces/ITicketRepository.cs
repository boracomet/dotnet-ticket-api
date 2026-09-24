using TicketApi.Application.Common;
using TicketApi.Application.DTOs;
using TicketApi.Domain.Entities;

namespace TicketApi.Application.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Ticket>> QueryAsync(TicketQuery query, Guid? restrictToUserId, CancellationToken ct = default);
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task UpdateAsync(Ticket ticket, CancellationToken ct = default);
    Task DeleteAsync(Ticket ticket, CancellationToken ct = default);
    Task<IReadOnlyList<TicketReply>> ListRepliesAsync(Guid ticketId, CancellationToken ct = default);
    Task<TicketReply?> GetReplyAsync(Guid replyId, CancellationToken ct = default);
    Task AddReplyAsync(TicketReply reply, CancellationToken ct = default);
}
