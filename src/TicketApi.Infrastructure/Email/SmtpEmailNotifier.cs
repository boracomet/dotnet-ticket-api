using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using TicketApi.Application.Interfaces;
using TicketApi.Domain.Entities;

namespace TicketApi.Infrastructure.Email;

public class SmtpEmailNotifier : IEmailNotifier
{
    private readonly ISmtpSettingsRepository _settings;
    private readonly IConfiguration _config;

    public SmtpEmailNotifier(ISmtpSettingsRepository settings, IConfiguration config)
    {
        _settings = settings;
        _config = config;
    }

    public async Task NotifyAsync(
        IReadOnlyList<EmailRecipient> recipients, string subject, string body, CancellationToken ct = default)
    {
        foreach (var recipient in recipients)
            await SendAsync(new[] { recipient }, subject, body, requireEnabled: true, useEnvFirst: true, ct);
    }

    public Task<string?> SendTestAsync(string toEmail, CancellationToken ct = default) =>
        // Test uses DB/form settings so the admin panel can try SMTP without requiring .env.
        SendAsync(new[] { new EmailRecipient(toEmail, toEmail) }, "SMTP test", "Ticket Board SMTP ayarı çalışıyor.", requireEnabled: false, useEnvFirst: false, ct);

    private async Task<string?> SendAsync(
        IReadOnlyList<EmailRecipient> recipients,
        string subject,
        string body,
        bool requireEnabled,
        bool useEnvFirst,
        CancellationToken ct)
    {
        var settings = useEnvFirst
            ? await ResolveNotifySettingsAsync(ct)
            : await _settings.GetAsync(ct);

        if (requireEnabled && !settings.Enabled) return null;
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.FromEmail))
            return "SMTP sunucusu veya gönderen adresi eksik";

        try
        {
#pragma warning disable SYSLIB0014
            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
#pragma warning restore SYSLIB0014
            if (!string.IsNullOrWhiteSpace(settings.Username))
                client.Credentials = new NetworkCredential(settings.Username, settings.Password);

            using var message = new MailMessage
            {
                From = new MailAddress(settings.FromEmail, settings.FromName),
                Subject = subject,
                Body = body
            };
            foreach (var recipient in recipients.Where(r => !string.IsNullOrWhiteSpace(r.Email)))
                message.To.Add(new MailAddress(recipient.Email, recipient.Name));
            if (message.To.Count == 0) return "Alıcı e-posta yok";

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            await client.SendMailAsync(message, timeout.Token);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Notifications prefer env/config when Smtp:Host is set; otherwise DB (admin form).
    /// Empty env password falls back to the stored DB password.
    /// </summary>
    private async Task<SmtpSettings> ResolveNotifySettingsAsync(CancellationToken ct)
    {
        var envHost = _config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(envHost))
            return await _settings.GetAsync(ct);

        var db = await _settings.GetAsync(ct);
        var envPassword = _config["Smtp:Password"];
        return new SmtpSettings
        {
            Host = envHost.Trim(),
            Port = ParsePort(_config["Smtp:Port"], 587),
            Username = (_config["Smtp:Username"] ?? string.Empty).Trim(),
            Password = string.IsNullOrEmpty(envPassword) ? db.Password : envPassword,
            FromEmail = (_config["Smtp:FromEmail"] ?? string.Empty).Trim(),
            FromName = string.IsNullOrWhiteSpace(_config["Smtp:FromName"])
                ? "Ticket Board"
                : _config["Smtp:FromName"]!.Trim(),
            EnableSsl = ParseBool(_config["Smtp:EnableSsl"], true),
            Enabled = ParseBool(_config["Smtp:Enabled"], false)
        };
    }

    private static int ParsePort(string? value, int fallback) =>
        int.TryParse(value, out var port) && port is >= 1 and <= 65535 ? port : fallback;

    private static bool ParseBool(string? value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
