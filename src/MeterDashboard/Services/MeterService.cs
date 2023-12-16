using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MeterDashboard.Services;

class MeterService : IDisposable
{
    private readonly MeterListener _meterListener;
    private readonly ActivityListener _activityListener;
    private readonly Storage _storage;

    public MeterService(Storage storage)
    {
        _storage = storage;
        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = InstrumentPublished;
        _meterListener.SetMeasurementEventCallback<byte>(MeasurementCallback);
        _meterListener.SetMeasurementEventCallback<short>(MeasurementCallback);
        _meterListener.SetMeasurementEventCallback<int>(MeasurementCallback);
        _meterListener.SetMeasurementEventCallback<long>(MeasurementCallback);
        _meterListener.SetMeasurementEventCallback<decimal>(MeasurementCallback);
        _meterListener.SetMeasurementEventCallback<float>(MeasurementCallback);
        _meterListener.SetMeasurementEventCallback<double>(MeasurementCallback);
        
        _activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = ActivityStopped
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Start()
    {
        _meterListener.Start();
    }
    
    private void ActivityStopped(Activity activity)
    {
        _storage.Insert(activity);
    }

    private void InstrumentPublished(Instrument instrument, MeterListener listener)
    {
        _meterListener.EnableMeasurementEvents(instrument);
    }

    private void MeasurementCallback(Instrument instrument, byte measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        _storage.Insert(instrument, measurement, tags);
    }    
    private void MeasurementCallback(Instrument instrument, short measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        _storage.Insert(instrument, measurement, tags);
    }   
    private void MeasurementCallback(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        _storage.Insert(instrument, measurement, tags);
    }   
    private void MeasurementCallback(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        _storage.Insert(instrument, measurement, tags);
    } 
    private void MeasurementCallback(Instrument instrument, decimal measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        _storage.Insert(instrument, measurement, tags);
    }
    private void MeasurementCallback(Instrument instrument, float measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        _storage.Insert(instrument, measurement, tags);
    }
    private void MeasurementCallback(Instrument instrument, double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        _storage.Insert(instrument, measurement, tags);
    }

    public void Dispose()
    {
        _meterListener.Dispose();
        _activityListener.Dispose();
    }
}