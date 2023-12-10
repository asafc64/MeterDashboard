namespace MeterDashboard.Web.Contracts;

public class MeasurementResponse
{
    public string MeterName { get; set; }
    public string InstrumentName { get; set; }
    public string InstrumentType { get; set; }
    public string? InstrumentUnit { get; set; }
    public IReadOnlyCollection<Metric> Metrics { get; set; }
    
    public class Metric
    {
        public Dictionary<string,object?> Tags { get; set; }
        public IReadOnlyCollection<DateTime> Xs { get; set; }
        public IReadOnlyCollection<object> Ys { get; set; }
    }
}