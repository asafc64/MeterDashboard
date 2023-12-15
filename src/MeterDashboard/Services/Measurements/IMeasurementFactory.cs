using System.Diagnostics.Metrics;

namespace MeterDashboard.Services.Measurements;

public interface IMeasurementFactory
{
    static abstract IMeasurement Create(ReadOnlySpan<KeyValuePair<string, object?>> tags, Instrument instrument);

}