using System.Diagnostics.Metrics;

namespace MeterDashboard.Services.Measurements;

public interface IMeasurementFactory
{
    static abstract IMeasurement Create(TimeSpan interval, int capacity,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, Instrument instrument);

}