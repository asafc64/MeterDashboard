using Microsoft.Extensions.Hosting;

namespace MeterDashboard.Services;

public class InitializerHostedService : IHostedService
{
    private readonly MeterService _meterService;

    public InitializerHostedService(MeterService meterService)
    {
        _meterService = meterService;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _meterService.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _meterService.Dispose();
        return Task.CompletedTask;
    }
}