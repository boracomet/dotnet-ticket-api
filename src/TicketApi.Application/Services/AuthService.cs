using TicketApi.Application.Common;
using TicketApi.Application.DTOs;
using TicketApi.Application.Interfaces;
using TicketApi.Domain.Entities;

namespace TicketApi.Application.Services;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasherService _hasher;
    private readonly IJwtTokenService _jwt;

    public AuthService(IUserRepository users, IPasswordHasherService hasher, IJwtTokenService jwt)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        ValidateRegister(request);
        if (await _users.ExistsByEmailAsync(request.Email, ct))
            throw new AppException(409, "EMAIL_TAKEN", "Email is already registered");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            Role = Roles.User,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _users.AddAsync(user, ct);
        return ToAuth(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct)
            ?? throw new AppException(401, "INVALID_CREDENTIALS", "Email or password is incorrect");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new AppException(401, "INVALID_CREDENTIALS", "Email or password is incorrect");

        return ToAuth(user);
    }

    private AuthResponse ToAuth(AppUser user) =>
        new(_jwt.CreateToken(user), "Bearer", user.Id, user.Email, user.FullName, user.Role);

    private static void ValidateRegister(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new AppException(400, "VALIDATION_ERROR", "Valid email is required");
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length < 2)
            throw new AppException(400, "VALIDATION_ERROR", "Full name is required");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new AppException(400, "VALIDATION_ERROR", "Password must be at least 8 characters");
    }
}
