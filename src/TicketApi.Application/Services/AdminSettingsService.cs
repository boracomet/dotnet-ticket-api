using TicketApi.Application.Common;
using TicketApi.Application.DTOs;
using TicketApi.Application.Interfaces;
using TicketApi.Domain.Entities;

namespace TicketApi.Application.Services;

public class AdminSettingsService
{
    private readonly ISmtpSettingsRepository _smtp;
    private readonly IEmailNotifier _email;

    public AdminSettingsService(ISmtpSettingsRepository smtp, IEmailNotifier email)
    {
        _smtp = smtp;
        _email = email;
    }

    public async Task<SmtpSettingsResponse> GetAsync(CancellationToken ct = default) =>
        ToResponse(await _smtp.GetAsync(ct));

    public async Task<SmtpSettingsResponse> UpdateAsync(UpdateSmtpSettingsRequest request, CancellationToken ct = default)
    {
        if (request.Port is < 1 or > 65535)
            throw new AppException(400, "VALIDATION_ERROR", "SMTP port is invalid");
        if (request.Enabled && string.IsNullOrWhiteSpace(request.Host))
            throw new AppException(400, "VALIDATION_ERROR", "SMTP host is required");
        if (request.Enabled && (string.IsNullOrWhiteSpace(request.FromEmail) || !request.FromEmail.Contains('@')))
            throw new AppException(400, "VALIDATION_ERROR", "From email is invalid");

        var current = await _smtp.GetAsync(ct);
        current.Host = request.Host?.Trim() ?? string.Empty;
        current.Port = request.Port;
        current.Username = request.Username?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(request.Password))
            current.Password = request.Password;
        current.FromEmail = request.FromEmail?.Trim() ?? string.Empty;
        current.FromName = string.IsNullOrWhiteSpace(request.FromName) ? "Ticket Board" : request.FromName.Trim();
        current.EnableSsl = request.EnableSsl;
        current.Enabled = request.Enabled;
        await _smtp.SaveAsync(current, ct);
        return ToResponse(current);
    }

    public async Task SendTestAsync(string toEmail, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains('@'))
            throw new AppException(400, "VALIDATION_ERROR", "Test email is invalid");

        var error = await _email.SendTestAsync(toEmail.Trim(), ct);
        if (error != null)
            throw new AppException(400, "SMTP_ERROR", error);
    }

    private static SmtpSettingsResponse ToResponse(SmtpSettings settings) => new(
        settings.Host,
        settings.Port,
        settings.Username,
        !string.IsNullOrEmpty(settings.Password),
        settings.FromEmail,
        settings.FromName,
        settings.EnableSsl,
        settings.Enabled);
}
