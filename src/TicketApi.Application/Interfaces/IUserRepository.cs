using TicketApi.Domain.Entities;

namespace TicketApi.Application.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<AppUser>> ListByRoleAsync(string role, CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
}
