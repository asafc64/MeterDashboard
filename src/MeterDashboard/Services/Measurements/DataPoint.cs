namespace MeterDashboard.Services.Measurements;

public struct DataPoint
{
    public DateTime Timestamp;
    public object Value;
}

public struct DataPoint<T>
{
    public DateTime Timestamp;
    public T Value;

    public DataPoint(DateTime timestamp)
    {
        Timestamp = timestamp;
    }

    public override string ToString() => $"({Timestamp}, {Value})";
}