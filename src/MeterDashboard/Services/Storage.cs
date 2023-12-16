using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using MeterDashboard.Services.Measurements;

namespace MeterDashboard.Services;

class Storage
{
    private readonly ITimeLineFactory _timeLineFactory;
    private readonly Dictionary<long, IMeasurement> _measurements = new();

    public Storage(ITimeLineFactory timeLineFactory)
    {
        _timeLineFactory = timeLineFactory;
    }
    
    public IReadOnlyCollection<IMeasurement> Measurements => _measurements.Values;

    public void Insert<T>(Instrument instrument, T value, ReadOnlySpan<KeyValuePair<string, object?>> tags) 
        where T : struct
    {
        switch (instrument)
        {
            case Counter<T>: 
                switch (value)
                {
                    case byte byteValue:
                        GetMeasurement<CounterMeasurement<byte>>(instrument, tags).Add(byteValue);
                        break;
                    case short shortValue:
                        GetMeasurement<CounterMeasurement<short>>(instrument, tags).Add(shortValue);
                        break;
                    case int intValue:
                        GetMeasurement<CounterMeasurement<int>>(instrument, tags).Add(intValue);
                        break;
                    case long longValue:
                        GetMeasurement<CounterMeasurement<long>>(instrument, tags).Add(longValue);
                        break;
                    case float floatValue:
                        GetMeasurement<CounterMeasurement<float>>(instrument, tags).Add(floatValue);
                        break;
                    case double doubleValue:
                        GetMeasurement<CounterMeasurement<double>>(instrument, tags).Add(doubleValue);
                        break;
                    case decimal decimalValue:
                        GetMeasurement<CounterMeasurement<decimal>>(instrument, tags).Add(decimalValue);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported value type '{value.GetType().FullName}'");
                }   
                break;
            case UpDownCounter<T>:
                switch (value)
                {
                    case byte byteValue:
                        GetMeasurement<UpDownCounterMeasurement<byte>>(instrument, tags).Add(byteValue);
                        break;
                    case short shortValue:
                        GetMeasurement<UpDownCounterMeasurement<short>>(instrument, tags).Add(shortValue);
                        break;
                    case int intValue:
                        GetMeasurement<UpDownCounterMeasurement<int>>(instrument, tags).Add(intValue);
                        break;
                    case long longValue:
                        GetMeasurement<UpDownCounterMeasurement<long>>(instrument, tags).Add(longValue);
                        break;
                    case float floatValue:
                        GetMeasurement<UpDownCounterMeasurement<float>>(instrument, tags).Add(floatValue);
                        break;
                    case double doubleValue:
                        GetMeasurement<UpDownCounterMeasurement<double>>(instrument, tags).Add(doubleValue);
                        break;
                    case decimal decimalValue:
                        GetMeasurement<UpDownCounterMeasurement<decimal>>(instrument, tags).Add(decimalValue);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported value type '{value.GetType().FullName}'");
                }   
                break;
            case Histogram<T>:
                switch (value)
                {
                    case byte byteValue:
                        GetMeasurement<HistogramMeasurement<byte, DoubleConverters.Byte>>(instrument, tags).Add(byteValue);
                        break;
                    case short shortValue:
                        GetMeasurement<HistogramMeasurement<short, DoubleConverters.Short>>(instrument, tags).Add(shortValue);
                        break;
                    case int intValue:
                        GetMeasurement<HistogramMeasurement<int, DoubleConverters.Int>>(instrument, tags).Add(intValue);
                        break;
                    case long longValue:
                        GetMeasurement<HistogramMeasurement<long, DoubleConverters.Long>>(instrument, tags).Add(longValue);
                        break;
                    case float floatValue:
                        GetMeasurement<HistogramMeasurement<float, DoubleConverters.Float>>(instrument, tags).Add(floatValue);
                        break;
                    case double doubleValue:
                        GetMeasurement<HistogramMeasurement<double, DoubleConverters.Double>>(instrument, tags).Add(doubleValue);
                        break;
                    case decimal decimalValue:
                        GetMeasurement<HistogramMeasurement<decimal, DoubleConverters.Decimal>>(instrument, tags).Add(decimalValue);
                        break;
                    case TimeSpan tsValue:
                        GetMeasurement<HistogramMeasurement<TimeSpan, DoubleConverters.TimeSpan>>(instrument, tags).Add(tsValue);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported value type '{value.GetType().FullName}'");
                }   
                break;
        }
    }

    public void Insert(Activity activity)
    {
        GetMeasurement(activity.Source.Name, activity.OperationName).Add(activity);
    }

    private ActivityMeasurement GetMeasurement(string sourceName, string activityName)
    {
        var key = sourceName.GetHashCode() ^
                  activityName.GetHashCode();
        
        if (!_measurements.TryGetValue(key, out var measurement))
        {
            lock (_measurements)
            {
                if (!_measurements.TryGetValue(key, out measurement))
                {
                    measurement = new ActivityMeasurement(sourceName, activityName, _timeLineFactory);
                    _measurements[key] = measurement;
                }
            }
        }

        return (ActivityMeasurement)measurement;
    }
    
    private TMeasurement GetMeasurement<TMeasurement>(Instrument instrument, ReadOnlySpan<KeyValuePair<string, object?>> tags) 
        where TMeasurement : IMeasurement, IMeasurementFactory
    {
        var key = instrument.Meter.Name.GetHashCode() ^
                  instrument.Name.GetHashCode();
        foreach (ref readonly var tag in tags)
        {
            key ^= tag.Key.GetHashCode();
            if (tag.Value != null) 
                key ^= tag.Value!.GetHashCode();
        }

        if (!_measurements.TryGetValue(key, out var measurement))
        {
            lock (_measurements)
            {
                if (!_measurements.TryGetValue(key, out measurement))
                {
                    measurement = TMeasurement.Create(tags, instrument, _timeLineFactory);
                    _measurements[key] = measurement;
                }
            }
        }
       
        return (TMeasurement)measurement;
    }
}