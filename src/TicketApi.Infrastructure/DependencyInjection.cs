using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketApi.Application.Interfaces;
using TicketApi.Infrastructure.Email;
using TicketApi.Infrastructure.Identity;
using TicketApi.Infrastructure.Persistence;
using TicketApi.Infrastructure.Repositories;
using TicketApi.Infrastructure.Storage;

namespace TicketApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Database:Provider"] ?? "Postgres";
        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase)
            || string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            var sqlite = config.GetConnectionString("Sqlite") ?? "Data Source=ticketapi.db";
            if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TicketApiTests"));
            }
            else
            {
                services.AddDbContext<AppDbContext>(o => o.UseSqlite(sqlite));
            }
        }
        else
        {
            var cs = config.GetConnectionString("Default")
                ?? "Host=localhost;Port=5432;Database=ticket_api;Username=ticket;Password=ticket_secret";
            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(cs));
        }

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ISmtpSettingsRepository, SmtpSettingsRepository>();
        services.AddScoped<IReplyFileStore, ReplyFileStore>();
        services.AddScoped<IEmailNotifier, SmtpEmailNotifier>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        return services;
    }
}
