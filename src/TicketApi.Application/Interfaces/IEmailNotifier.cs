namespace TicketApi.Application.Interfaces;

public record EmailRecipient(string Email, string Name);

public interface IEmailNotifier
{
    Task NotifyAsync(IReadOnlyList<EmailRecipient> recipients, string subject, string body, CancellationToken ct = default);
    Task<string?> SendTestAsync(string toEmail, CancellationToken ct = default);
}
