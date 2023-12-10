using System.Diagnostics.Metrics;

namespace MeterDashboard.Services.Measurements;

public struct Stat<T> where T: struct
{
    public T Min;
    public T Max;
    public T Mean;
    public T Stddev;
}

public interface IDoubleConverter<T>
{
    double ToDouble(T value);
    T FromDouble(double value);
}

public static class DoubleConverters
{
    public class Byte : IDoubleConverter<byte>
    {
        public double ToDouble(byte value) => value;
        public byte FromDouble(double value) => (byte) value;
    }

    public class Short : IDoubleConverter<short>
    {
        public double ToDouble(short value) => value;
        public short FromDouble(double value) => (short) value;
    }

    public class Int : IDoubleConverter<int>
    {
        public double ToDouble(int value) => value;
        public int FromDouble(double value) => (int) value;
    } 
    
    public class Long : IDoubleConverter<long>
    {
        public double ToDouble(long value) => value;
        public long FromDouble(double value) => (long) value;
    } 
    
    public class Float : IDoubleConverter<float>
    {
        public double ToDouble(float value) => value;
        public float FromDouble(double value) => (float) value;
    }   
    
    public class Double : IDoubleConverter<double>
    {
        public double ToDouble(double value) => value;
        public double FromDouble(double value) => value;
    }   
    
    public class Decimal : IDoubleConverter<decimal>
    {
        public double ToDouble(decimal value) => (double)value;
        public decimal FromDouble(double value) => (decimal)value;
    } 
    
    public class TimeSpan : IDoubleConverter<System.TimeSpan>
    {
        public double ToDouble(System.TimeSpan value) => value.Ticks;
        public System.TimeSpan FromDouble(double value) => new ((long)value);
    } 
}


public class HistogramMeasurement<T, C> : IMeasurement, IMeasurementFactory
    where T: struct 
    where C: IDoubleConverter<T>, new()
{
    private TimeLine<DoubleStat> _timeLine;
    private C _converter = new();

    public HistogramMeasurement(TimeSpan interval, int capacity, ReadOnlySpan<KeyValuePair<string, object?>> tags, Instrument instrument)
    {
        Instrument = instrument;
        Tags = tags.ToArray();
        _timeLine = new TimeLine<DoubleStat>(interval, capacity, false);
    }

    public static IMeasurement Create(TimeSpan interval, int capacity, ReadOnlySpan<KeyValuePair<string, object?>> tags,
        Instrument instrument)
    {
        return new HistogramMeasurement<T, C>(interval, capacity, tags, instrument);
    }

    public Instrument Instrument { get; }
    
    public KeyValuePair<string, object?>[] Tags { get; }
    
    public DataPoint[] GetDataPoints(DateTime? since)
    {
        var items = _timeLine.SnapShot();
        return items
            .Where(x => since == null || x.Timestamp > since)
            .Select(item => new DataPoint
            {
                Timestamp = item.Timestamp,
                Value = new Stat<T>()
                {
                    Min = _converter.FromDouble(item.Value.Min),
                    Max = _converter.FromDouble(item.Value.Max),
                    Mean = _converter.FromDouble(item.Value.GetMean()),
                    Stddev = _converter.FromDouble(item.Value.GetStddev())
                }
            })
            .ToArray();
    }

    public DataPoint[] GetMergedDataPoints(IReadOnlyCollection<IMeasurement> others, DateTime? since)
    {
        throw new NotImplementedException();
    }

    public void Add(T tvalue)
    {
        var value = _converter.ToDouble(tvalue);
        lock (_timeLine)
        {
            _timeLine.GetOrAdd(DateTime.UtcNow).Value.Add(value);
        }
    }

    private struct DoubleStat
    {
        public long N;
        public double Sum;
        public double SqrSum;
        public double Min;
        public double Max;

        public void Add(double value)
        {
            N++;
            Sum += value;
            SqrSum += value*value;
            if (value < Min || N == 0)
                Min = value;
            if (value > Max || N == 0)
                Max = value;
        }
        
        public readonly double GetStddev() => Math.Sqrt(SqrSum / N - Math.Pow(GetMean(), 2));

        public readonly double GetMean() => Sum / N;

        public override string ToString() => $"{GetMean():F3}+-{GetStddev():F3} [{Min:F3},{Max:F3}]";
    }
}