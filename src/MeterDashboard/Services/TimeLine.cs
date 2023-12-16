using MeterDashboard.Services.Measurements;
using MeterDashboard.Utils;
using Microsoft.Extensions.Options;

namespace MeterDashboard.Services;

interface ITimeLineFactory
{
    TimeLine<T> Create<T>(bool aggValue) where T : struct;
}

class TimeLineFactory : ITimeLineFactory
{
    private readonly IOptions<MeterDashboardOptions> _options;

    public TimeLineFactory(IOptions<MeterDashboardOptions> options)
    {
        _options = options;
    }
    
    public TimeLine<T> Create<T>(bool aggValue) where T : struct
    {
        return new TimeLine<T>(aggValue, _options.Value.Seconds, _options.Value.Minutes);
    }
}

class TimeLine<T> where T: struct
{
    private readonly bool _aggValue;
    private readonly CircularQueue<T> _queueOfSeconds;
    private readonly CircularQueue<T> _queueOfMinutes;

    public delegate void Updater(ref T exitingValue, in T newValue);

    public TimeLine(bool aggValue, int seconds, int minutes)
    {
        _aggValue = aggValue;
        _queueOfSeconds = new CircularQueue<T>(TimeSpan.FromSeconds(1), seconds, aggValue);
        _queueOfMinutes = new CircularQueue<T>(TimeSpan.FromMinutes(1), minutes, aggValue);
    }

    public void AddOrUpdate(DateTime timestamp, T value, Updater update)
    {
        ref var item = ref _queueOfSeconds.GetOrAdd(timestamp);
        update(ref item.Value, value);
        
        item = ref _queueOfMinutes.GetOrAdd(timestamp);
        update(ref item.Value, value);
    }

    public IReadOnlyCollection<DataPoint<T>> SnapShot()
    {
        var results = new List<DataPoint<T>>(_queueOfSeconds.Capacity+_queueOfMinutes.Capacity);
        AddQueueSnapshot(results, _queueOfSeconds, Window.Second, _aggValue);
        AddQueueSnapshot(results, _queueOfMinutes, Window.Minute, _aggValue);
        return results;
    }

    private static void AddQueueSnapshot(List<DataPoint<T>> results, CircularQueue<T> queue, Window window, bool aggValue)
    {
        var items = queue.SnapShot();
        var itemIdx = 0;
        var roundedNow = queue.RoundTimestamp(DateTime.UtcNow);
        var start = roundedNow - queue.Capacity*queue.Step;
        var lastValue = default(T);

        if(aggValue)
            for (; itemIdx < items.Length && items[itemIdx].Timestamp < start; itemIdx++)
            {
                lastValue = items[itemIdx].Value;
            }
        
        for (var time = start; time < roundedNow; time += 1.Seconds())
        {
            while (itemIdx < items.Length && 
                   items[itemIdx].Timestamp < time) itemIdx++;

            lastValue = itemIdx < items.Length && items[itemIdx].Timestamp == time
                ? items[itemIdx].Value 
                : (aggValue ? lastValue : default);
            
            results.Add(new DataPoint<T>(time, lastValue, window));
        }
    }
}

public class CircularQueue<T>
{
    private TimeSpan _interval;
    private Item<T>[] _queue;
    private int _head = -1;
    private bool _isFull;
    private bool _aggValue;

    public CircularQueue(TimeSpan interval, int capacity, bool aggValue)
    {
        _interval = interval;
        _aggValue = aggValue;
        _queue = new Item<T>[capacity];
    }

    public int Capacity => _queue.Length;
    public TimeSpan Step => _interval;
    
    public ref Item<T> GetOrAdd(DateTime timestamp)
    {
        var roundedTimestamp = RoundTimestamp(timestamp); 
        
        if (_head == -1)
        {
            _head++;
            _queue[_head] = new Item<T>(roundedTimestamp);
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
            _queue[_head] = new Item<T>(roundedTimestamp);
            if(_aggValue)
                _queue[_head].Value = _queue[oldHead].Value;
        }

        return ref _queue[_head];
    }

    public Item<T>[] SnapShot()
    {
        if (_isFull)
        {
            var points = new Item<T>[_queue.Length];
            for (var i = 0; i < _queue.Length; i++)
            {
                points[i] = _queue[(_head + i + 1) % _queue.Length];
            }

            return points;
        }
        else
        {
            var points = new Item<T>[_head+1];
            for (var i = 0; i <= _head; i++)
            {
                points[i] = _queue[i];
            }

            return points;
        }
    }

    public DateTime RoundTimestamp(DateTime timestamp)
    {
        return new DateTime(timestamp.Ticks - timestamp.Ticks % _interval.Ticks);
    }
    
    public struct Item<T>
    {
        public DateTime Timestamp;
        public T Value;

        public Item(DateTime timestamp)
        {
            Timestamp = timestamp;
        }

        public override string ToString() => $"({Timestamp}, {Value})";
    }
}