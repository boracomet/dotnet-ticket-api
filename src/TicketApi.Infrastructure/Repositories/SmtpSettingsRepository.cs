using Microsoft.EntityFrameworkCore;
using TicketApi.Application.Interfaces;
using TicketApi.Domain.Entities;
using TicketApi.Infrastructure.Persistence;

namespace TicketApi.Infrastructure.Repositories;

public class SmtpSettingsRepository : ISmtpSettingsRepository
{
    private readonly AppDbContext _db;

    public SmtpSettingsRepository(AppDbContext db) => _db = db;

    public async Task<SmtpSettings> GetAsync(CancellationToken ct = default)
    {
        var row = await _db.SmtpSettings.FirstOrDefaultAsync(s => s.Id == SmtpSettings.SingletonId, ct);
        if (row != null) return row;

        row = new SmtpSettings { Id = SmtpSettings.SingletonId };
        _db.SmtpSettings.Add(row);
        await _db.SaveChangesAsync(ct);
        return row;
    }

    public async Task SaveAsync(SmtpSettings settings, CancellationToken ct = default)
    {
        var existing = await _db.SmtpSettings.FirstOrDefaultAsync(s => s.Id == settings.Id, ct);
        if (existing == null)
        {
            _db.SmtpSettings.Add(settings);
        }
        else
        {
            existing.Host = settings.Host;
            existing.Port = settings.Port;
            existing.Username = settings.Username;
            existing.Password = settings.Password;
            existing.FromEmail = settings.FromEmail;
            existing.FromName = settings.FromName;
            existing.EnableSsl = settings.EnableSsl;
            existing.Enabled = settings.Enabled;
        }

        await _db.SaveChangesAsync(ct);
    }
}
