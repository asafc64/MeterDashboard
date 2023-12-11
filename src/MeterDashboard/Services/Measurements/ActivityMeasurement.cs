using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MeterDashboard.Services.Measurements;

public struct ActivityDataPointValue
{
    public int Occurrances { get; init; }
    public double MeanDuration { get; init; }
    public double StddevDuration { get; init; }
} 

public class ActivityMeasurement : IMeasurement
{
    private TimeLine<Stats> _timeLine;

    public ActivityMeasurement(TimeSpan interval, int capacity, string sourceName, string activityName)
    {
        _timeLine = new TimeLine<Stats>(interval, capacity, false);
        Instrument = new ActivityInstrument(sourceName, activityName);
    }

    public void Add(Activity activity)
    {
        lock (_timeLine)
        {
            _timeLine.GetOrAdd(DateTime.UtcNow).Value.Add(activity.Duration);
        }
    }

    public Instrument Instrument { get; }
    
    public KeyValuePair<string, object?>[] Tags { get; } = Array.Empty<KeyValuePair<string, object?>>();
    
    public DataPoint[] GetDataPoints(DateTime? since)
    {
        var items = _timeLine.SnapShot();
        return items
            .Select(i => new DataPoint
            {
                Timestamp = i.Timestamp,
                Value = new ActivityDataPointValue
                {
                    Occurrances = i.Value.N,
                    MeanDuration = i.Value.GetMean(),
                    StddevDuration = i.Value.GetStddev(),
                }
            })
            .ToArray();
    }

    public DataPoint[] GetMergedDataPoints(IReadOnlyCollection<IMeasurement> others, DateTime? since)
    {
        throw new NotImplementedException();
    }

    private struct Stats
    {
        public double Sum;
        public double SqrSum;
        public int N;

        public void Add(TimeSpan duration)
        {
            Sum += duration.Ticks;
            SqrSum += duration.Ticks*duration.Ticks;
            N++;
        }
        
        public readonly double GetStddev() => N > 0 ? Math.Sqrt(SqrSum / N - Math.Pow(GetMean(), 2)) : 0;

        public readonly double GetMean() => N > 0 ? Sum / N : 0;

        public override string ToString() => $"{GetMean():F3}+-{GetStddev():F3}";
    }
    
    private class ActivityInstrument : Instrument
    {
        public ActivityInstrument(string sourceName, string activityName) 
            : base(new Meter(sourceName), activityName, null, null)
        {
        }
    }
}