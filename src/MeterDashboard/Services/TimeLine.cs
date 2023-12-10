using MeterDashboard.Services.Measurements;

namespace MeterDashboard.Services;

public class TimeLine<T>
{
    private TimeSpan _interval;
    private DataPoint<T>[] _queue;
    private int _head = -1;
    private bool _isFull;
    private bool _aggValue;

    public TimeLine(TimeSpan interval, int capacity, bool aggValue)
    {
        _interval = interval;
        _aggValue = aggValue;
        _queue = new DataPoint<T>[capacity];
    }

    public ref DataPoint<T> GetOrAdd(DateTime timestamp)
    {
        var roundedTimestamp = RoundTimestamp(timestamp); 
        
        if (_head == -1)
        {
            _head++;
            _queue[_head] = new DataPoint<T>(roundedTimestamp);
            return ref _queue[_head];
        }
 
        if (roundedTimestamp > _queue[_head].Timestamp)
        {
            var oldHead = _head;
            _head++;
            if (_head == _queue.Length)
            {
                _head = 0;
                _isFull = true;
            }
            _queue[_head] = new DataPoint<T>(roundedTimestamp);
            if(_aggValue)
                _queue[_head].Value = _queue[oldHead].Value;
        }

        return ref _queue[_head];
    }

    public DataPoint<T>[] SnapShot()
    {
        if (_isFull)
        {
            var points = new DataPoint<T>[_queue.Length];
            for (var i = 0; i < _queue.Length; i++)
            {
                points[i] = _queue[(_head + i + 1) % _queue.Length];
            }

            return points;
        }
        else
        {
            var points = new DataPoint<T>[_head+1];
            for (var i = 0; i <= _head; i++)
            {
                points[i] = _queue[i];
            }

            return points;
        }
    }

    private DateTime RoundTimestamp(DateTime timestamp)
    {
        return new DateTime(timestamp.Ticks - timestamp.Ticks % _interval.Ticks);
    }
}