namespace MeterDashboard.Utils;

static class TimeSpanExtensions
{
    public static TimeSpan Seconds<T>(this T source) where T : IConvertible => TimeSpan.FromSeconds(source.ToDouble(null));
    public static TimeSpan Minutes<T>(this T source) where T : IConvertible => TimeSpan.FromMinutes(source.ToDouble(null));
    public static TimeSpan Milliseconds<T>(this T source) where T : IConvertible => TimeSpan.FromMilliseconds(source.ToDouble(null));
}