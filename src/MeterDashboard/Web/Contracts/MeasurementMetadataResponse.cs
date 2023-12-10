namespace MeterDashboard.Web.Contracts;

public class MeasurementMetadataResponse
{
    public string MeterName { get; set; }
    public string InstrumentName { get; set; }
    public string? InstrumentUnit { get; set; }
}