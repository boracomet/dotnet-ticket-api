namespace TicketApi.Domain.Entities;

public class AppUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.User;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
