using System.Diagnostics.Metrics;

namespace MeterDashboard.Services.Measurements;

public record struct Stat<T> where T: struct
{
    public T Min { get; init; }
    public T Max { get; init; }
    public T Mean { get; init; }
    public T Stddev { get; init; }
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


class HistogramMeasurement<T, C> : IMeasurement, IMeasurementFactory
    where T: struct 
    where C: IDoubleConverter<T>, new()
{
    private TimeLine<DoubleStat> _timeLine;
    private C _converter = new();

    public HistogramMeasurement(ReadOnlySpan<KeyValuePair<string, object?>> tags, Instrument instrument,
        ITimeLineFactory timeLineFactory)
    {
        Instrument = instrument;
        Tags = tags.ToArray();
        _timeLine = timeLineFactory.Create<DoubleStat>(false);
    }

    public static IMeasurement Create(ReadOnlySpan<KeyValuePair<string, object?>> tags,
        Instrument instrument, ITimeLineFactory timeLineFactory)
    {
        return new HistogramMeasurement<T, C>(tags, instrument, timeLineFactory);
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
                Window = item.Window,
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
        var stats = new DoubleStat();
        stats.Add(_converter.ToDouble(tvalue));
        
        _timeLine.AddOrUpdate(
            DateTime.UtcNow, 
            stats,
            (ref DoubleStat exitingValue, in DoubleStat newValue) => exitingValue.Add(newValue));
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
        
        public void Add(DoubleStat other)
        {
            N += other.N;
            Sum += other.Sum;
            SqrSum += other.SqrSum;
            if (other.Min < Min || N == 0)
                Min = other.Min;
            if (other.Max > Max || N == 0)
                Max = other.Max;
        }
        
        public readonly double GetStddev() => N > 0 ? Math.Sqrt(SqrSum / N - Math.Pow(GetMean(), 2)) : 0;

        public readonly double GetMean() => N > 0 ? Sum / N : 0;

        public override string ToString() => $"{GetMean():F3}+-{GetStddev():F3} [{Min:F3},{Max:F3}]";
    }
}