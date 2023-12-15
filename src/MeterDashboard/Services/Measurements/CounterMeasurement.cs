using System.Diagnostics.Metrics;
using System.Numerics;

namespace MeterDashboard.Services.Measurements;

public class CounterMeasurement<T> : IMeasurement, IMeasurementFactory where T: struct, IAdditionOperators<T,T,T>
{
    private TimeLine<T> _timeLine;

    public CounterMeasurement(ReadOnlySpan<KeyValuePair<string, object?>> tags, Instrument instrument)
    {
        Instrument = instrument;
        Tags = tags.ToArray();
        _timeLine = new TimeLine<T>(false);
    }
    
    public static IMeasurement Create(ReadOnlySpan<KeyValuePair<string, object?>> tags, Instrument instrument)
    {
        return new CounterMeasurement<T>(tags, instrument);
    }

    public Instrument Instrument { get; }

    public KeyValuePair<string, object?>[] Tags { get; }

    public DataPoint[] GetDataPoints(DateTime? since)
    {
        return _timeLine.SnapShot()
            .Where(x => since == null || x.Timestamp > since)
            .Select(x => new DataPoint
            {
                Timestamp = x.Timestamp,
                Window = x.Window,
                Value = x.Value
            })
            .ToArray();
    }

    public DataPoint[] GetMergedDataPoints(IReadOnlyCollection<IMeasurement> others, DateTime? since)
    {
        throw new NotImplementedException();
    }

    public void Add(T value)
    {
        _timeLine.AddOrUpdate(
            DateTime.UtcNow, 
            value, 
            (ref T exitingValue, in T newValue) => exitingValue += newValue);
    }
}