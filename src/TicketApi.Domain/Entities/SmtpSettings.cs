namespace TicketApi.Domain.Entities;

public class SmtpSettings
{
    public static readonly Guid SingletonId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public Guid Id { get; set; } = SingletonId;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Ticket Board";
    public bool EnableSsl { get; set; } = true;
    public bool Enabled { get; set; }
}
