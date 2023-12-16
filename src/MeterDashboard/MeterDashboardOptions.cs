namespace MeterDashboard;

public record class MeterDashboardOptions
{
    public int Seconds { get; set; } = 60;
    public int Minutes { get; set; } = 60;
}