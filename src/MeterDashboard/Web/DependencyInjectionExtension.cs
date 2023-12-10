using MeterDashboard.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MeterDashboard.Web;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddMeterDashboard(this IServiceCollection services)
    {
        services.AddSingleton<Storage>();
        services.AddSingleton<MeterService>();
        services.AddHostedService<InitializerHostedService>();
        services.AddSingleton<MeterMiddleware>();
        return services;
    }
    
    public static void UseMeterDashboard(this IApplicationBuilder builder, string baseRoute = "/meter-dashboard")
    {
        builder.UseMiddleware<MeterMiddleware>();
    }
}