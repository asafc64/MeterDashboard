using Microsoft.Extensions.DependencyInjection;

namespace MeterDashboard.Services;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddMeterDashboard(this IServiceCollection services)
    {
        services.AddSingleton<Storage>();
        services.AddSingleton<MeterService>();
        services.AddHostedService<InitializerHostedService>();
        return services;
    }
}