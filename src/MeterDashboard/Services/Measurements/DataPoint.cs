namespace MeterDashboard.Services.Measurements;

public struct DataPoint
{
    public DateTime Timestamp;
    public Window Window;
    public object Value;
}

public record struct DataPoint<T>(DateTime Timestamp, T Value, Window Window);

public enum Window
{
    Second,
    Minute
}