using System.Diagnostics.Metrics;

namespace MeterDashboard.Services.Measurements;

public interface IMeasurement
{
    Instrument Instrument { get; }
    
    KeyValuePair<string, object?>[] Tags { get; }

    DataPoint[] GetDataPoints(DateTime? since);
    
    DataPoint[] GetMergedDataPoints(IReadOnlyCollection<IMeasurement> others, DateTime? since);
}