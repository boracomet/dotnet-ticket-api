using TicketApi.Domain.Entities;

namespace TicketApi.Application.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(AppUser user);
}
