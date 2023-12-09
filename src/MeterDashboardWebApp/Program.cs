using System.Diagnostics;
using System.Diagnostics.Metrics;
using MeterDashboard.Services;
using MeterDashboard.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<Generator>();
builder.Services.AddMeterDashboard();
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(o => o.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.MapMeterDashboard();

app.MapGet("/", () => "Hello World!");

app.Run();


class Generator : BackgroundService
{
    private static readonly Meter _meter = new("bank");
    private static readonly UpDownCounter<double> _expenses = _meter.CreateUpDownCounter<double>("expenses");
    private static readonly UpDownCounter<double> _stocks = _meter.CreateUpDownCounter<double>("stocks");
    private static readonly Counter<int> _transactions = _meter.CreateCounter<int>("transactions");
    
    private static readonly Meter _meter2 = new("api");
    private static readonly Counter<int> _calls = _meter2.CreateCounter<int>("calls");

    private static readonly ActivitySource _activitySource = new("Generator");
    
    private readonly Random _random = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = _activitySource.StartActivity();
            await Task.Delay(1000, stoppingToken);
            _expenses.Add(_random.NextDouble()*10-5);
            _stocks.Add(_random.NextDouble()*100-50, new KeyValuePair<string, object?>("User", "A"));
            _stocks.Add(_random.NextDouble()*100-50, new KeyValuePair<string, object?>("User", "B"));
            _transactions.Add((int)Math.Pow(_random.NextDouble()*10, 3));
            _calls.Add((int)Math.Pow(_random.NextDouble()*7, 3));
        }
    }
}