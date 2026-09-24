namespace TicketApi.Infrastructure.Email;

/// <summary>
/// Bound from configuration section "Smtp" (env: Smtp__Host, Smtp__Port, …).
/// Preferred source for production notification mail; admin form uses DB for local tests.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Ticket Board";
    public bool EnableSsl { get; set; } = true;
    public bool Enabled { get; set; }
}
