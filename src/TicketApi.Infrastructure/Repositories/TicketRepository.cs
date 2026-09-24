using Microsoft.EntityFrameworkCore;
using TicketApi.Application.Common;
using TicketApi.Application.DTOs;
using TicketApi.Application.Interfaces;
using TicketApi.Domain.Entities;
using TicketApi.Infrastructure.Persistence;

namespace TicketApi.Infrastructure.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _db;

    public TicketRepository(AppDbContext db) => _db = db;

    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Tickets.Include(t => t.Attachments).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<Ticket>> QueryAsync(
        TicketQuery query, Guid? restrictToUserId, CancellationToken ct = default)
    {
        var q = _db.Tickets.AsNoTracking().AsQueryable();

        if (restrictToUserId.HasValue)
        {
            var uid = restrictToUserId.Value;
            q = q.Where(t => t.CreatedByUserId == uid || t.AssignedToUserId == uid);
        }

        if (query.Status.HasValue)
            q = q.Where(t => t.Status == query.Status);
        if (query.Priority.HasValue)
            q = q.Where(t => t.Priority == query.Priority);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(t => t.Title.ToLower().Contains(s) || t.Description.ToLower().Contains(s));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Ticket>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        _db.Tickets.Update(ticket);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Ticket ticket, CancellationToken ct = default)
    {
        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TicketReply>> ListRepliesAsync(Guid ticketId, CancellationToken ct = default) =>
        await _db.TicketReplies.AsNoTracking()
            .Include(r => r.Author)
            .Include(r => r.Attachments)
            .Where(r => r.TicketId == ticketId)
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<TicketReply?> GetReplyAsync(Guid replyId, CancellationToken ct = default) =>
        _db.TicketReplies
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == replyId, ct);

    public async Task AddReplyAsync(TicketReply reply, CancellationToken ct = default)
    {
        _db.TicketReplies.Add(reply);
        await _db.SaveChangesAsync(ct);
    }
}
