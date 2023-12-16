using System.Diagnostics.Metrics;

namespace MeterDashboard.Services.Measurements;

interface IMeasurementFactory
{
    static abstract IMeasurement Create(ReadOnlySpan<KeyValuePair<string, object?>> tags, Instrument instrument, ITimeLineFactory timeLineFactory);

}