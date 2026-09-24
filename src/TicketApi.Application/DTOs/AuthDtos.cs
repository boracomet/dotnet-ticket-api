namespace TicketApi.Application.DTOs;

public record RegisterRequest(string Email, string FullName, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, string TokenType, Guid UserId, string Email, string FullName, string Role);
