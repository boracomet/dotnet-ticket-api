using TicketApi.Domain.Entities;

namespace TicketApi.Application.Interfaces;

public interface ISmtpSettingsRepository
{
    Task<SmtpSettings> GetAsync(CancellationToken ct = default);
    Task SaveAsync(SmtpSettings settings, CancellationToken ct = default);
}
