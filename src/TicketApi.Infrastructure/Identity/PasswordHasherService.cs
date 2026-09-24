using Microsoft.AspNetCore.Identity;
using TicketApi.Application.Interfaces;
using TicketApi.Domain.Entities;

namespace TicketApi.Infrastructure.Identity;

public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<AppUser> _hasher = new();

    public string Hash(string password) =>
        _hasher.HashPassword(new AppUser(), password);

    public bool Verify(string password, string hash) =>
        _hasher.VerifyHashedPassword(new AppUser(), hash, password)
            != PasswordVerificationResult.Failed;
}
