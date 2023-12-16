using MeterDashboard.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MeterDashboard.Web;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddMeterDashboard(this IServiceCollection services, Action<MeterDashboardOptions>? configure = null)
    {
        services.AddSingleton<ITimeLineFactory, TimeLineFactory>();
        services.AddSingleton<Storage>();
        services.AddSingleton<MeterService>();
        services.AddHostedService<InitializerHostedService>();
        services.AddSingleton<MeterMiddleware>();
        if (configure != null) 
            services.Configure(configure);
        return services;
    }
    
    public static void UseMeterDashboard(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<MeterMiddleware>();
    }
}