using Microsoft.EntityFrameworkCore;
using TicketApi.Application.Interfaces;
using TicketApi.Domain.Entities;
using TicketApi.Infrastructure.Persistence;

namespace TicketApi.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<IReadOnlyList<AppUser>> ListByRoleAsync(string role, CancellationToken ct = default) =>
        await _db.Users.AsNoTracking()
            .Where(u => u.Role == role)
            .ToListAsync(ct);

    public async Task AddAsync(AppUser user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }
}
