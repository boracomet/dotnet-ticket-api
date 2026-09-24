using Microsoft.Extensions.DependencyInjection;
using TicketApi.Application.Services;

namespace TicketApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<TicketService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<AdminSettingsService>();
        return services;
    }
}
