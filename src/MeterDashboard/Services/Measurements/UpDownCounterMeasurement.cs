using System.Diagnostics.Metrics;
using System.Numerics;
using MeterDashboard.Services.Measurements;

namespace MeterDashboard.Services;

public class UpDownCounterMeasurement<T> : IMeasurement, IMeasurementFactory where T: IAdditionOperators<T,T,T>
{
    private TimeLine<T> _timeLine;

    public UpDownCounterMeasurement(TimeSpan interval, int capacity, ReadOnlySpan<KeyValuePair<string, object?>> tags, Instrument instrument)
    {
        Instrument = instrument;
        Tags = tags.ToArray();
        _timeLine = new TimeLine<T>(interval, capacity, true);
    }
    
    public static IMeasurement Create(TimeSpan interval, int capacity,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, Instrument instrument)
    {
        return new UpDownCounterMeasurement<T>(interval, capacity, tags, instrument);
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
                Value = x.Value
            }).ToArray();
    }

    public DataPoint[] GetMergedDataPoints(IReadOnlyCollection<IMeasurement> others, DateTime? since)
    {
        throw new NotImplementedException();
    }

    public void Add(T value)
    {
        _timeLine.GetOrAdd(DateTime.UtcNow).Value += value;
    }
}