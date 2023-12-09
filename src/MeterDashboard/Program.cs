// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Numerics;
using MeterDashboard;
using MeterDashboard.Services;

Console.WriteLine("Hello, World!");

var meter = new Meter("bank");
var counter = meter.CreateUpDownCounter<double>("expenses");
var duration = meter.CreateHistogram<double>("duration");
var transactions = meter.CreateCounter<int>("transactions");
var rand = new Random();

using var meterService = new MeterService(new Storage());

var sw = Stopwatch.StartNew();
while(sw.Elapsed < TimeSpan.FromMinutes(1))
{
    counter.Add(rand.NextDouble()*100);   
    duration.Record(rand.NextDouble());
    Thread.Sleep((int)(rand.NextDouble()*100));
}

Console.WriteLine("Hello, World!");