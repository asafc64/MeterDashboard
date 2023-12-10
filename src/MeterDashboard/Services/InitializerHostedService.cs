using System.Collections.Immutable;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeterDashboard.Services;

public class InitializerHostedService : IHostedService
{
    private readonly MeterService _meterService;
    private readonly IServer _server;
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public InitializerHostedService(
        MeterService meterService, 
        IServer server,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        _meterService = meterService;
        _server = server;
        _configuration = configuration;
        _loggerFactory = loggerFactory;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _meterService.Start();
        LogDashboardAddress();
        return Task.CompletedTask;
    }

    private void LogDashboardAddress()
    {
        var serverAddressesFeature = _server.Features.Get<IServerAddressesFeature>();
        var addresses = serverAddressesFeature?.Addresses.ToImmutableList();
        if (addresses?.Any() != true)
        {
            var urls = _configuration[WebHostDefaults.ServerUrlsKey];
            addresses = urls?.Split(";").ToImmutableList();
        }

        var address = addresses?.FirstOrDefault();
        if (address?.Any() == true)
        {
            _loggerFactory
                .CreateLogger("MeterDashboard")
                .LogInformation($"MeterDashboard can be found in {address}/meter-dashboard");
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _meterService.Dispose();
        return Task.CompletedTask;
    }
}