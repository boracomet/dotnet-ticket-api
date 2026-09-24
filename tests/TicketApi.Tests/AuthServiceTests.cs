using FluentAssertions;
using Moq;
using TicketApi.Application.Common;
using TicketApi.Application.DTOs;
using TicketApi.Application.Interfaces;
using TicketApi.Application.Services;
using TicketApi.Domain.Entities;

namespace TicketApi.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasherService> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _jwt.Setup(j => j.CreateToken(It.IsAny<AppUser>())).Returns("token");
        _sut = new AuthService(_users.Object, _hasher.Object, _jwt.Object);
    }

    [Fact]
    public async Task Register_creates_user()
    {
        _users.Setup(u => u.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _users.Setup(u => u.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.RegisterAsync(new RegisterRequest("a@b.com", "Alice", "password123"));

        result.AccessToken.Should().Be("token");
        result.Email.Should().Be("a@b.com");
        result.Role.Should().Be(Roles.User);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        _users.Setup(u => u.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _sut.RegisterAsync(new RegisterRequest("a@b.com", "Alice", "password123"));
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.Code.Should().Be("EMAIL_TAKEN");
    }

    [Fact]
    public async Task Login_rejects_bad_password()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "a@b.com",
            PasswordHash = "hashed",
            FullName = "A",
            Role = Roles.User
        };
        _users.Setup(u => u.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("wrong", "hashed")).Returns(false);

        var act = async () => await _sut.LoginAsync(new LoginRequest("a@b.com", "wrong"));
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.Code.Should().Be("INVALID_CREDENTIALS");
    }
}
