using System.Runtime.InteropServices;
using MeterDashboard.Services.Measurements;
using MeterDashboard.Utils;

namespace MeterDashboard.Services;

public class TimeLine<T> where T: struct
{
    private readonly bool _aggValue;
    private readonly CircularQueue<T> _queueOfSeconds;
    private readonly CircularQueue<T> _queueOfMinutes;

    public delegate void Updater(ref T exitingValue, in T newValue);

    public TimeLine(bool aggValue)
    {
        _aggValue = aggValue;
        _queueOfSeconds = new CircularQueue<T>(TimeSpan.FromSeconds(1), 60, aggValue);
        _queueOfMinutes = new CircularQueue<T>(TimeSpan.FromMinutes(1), 60, aggValue);
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
        var itemsOfSeconds = _queueOfSeconds.SnapShot();
        var itemsOfMinutes = _queueOfMinutes.SnapShot();
        var nowSeconds = _queueOfSeconds.RoundTimestamp(DateTime.UtcNow);
        var nowMinutes = _queueOfMinutes.RoundTimestamp(DateTime.UtcNow);
        var results = new List<DataPoint<T>>(itemsOfSeconds.Length+itemsOfMinutes.Length);

        var itemIdx = 0;
        var start = nowSeconds - 60.Seconds();
        var lastValue = itemsOfSeconds.LastOrDefault(i => i.Timestamp < start).Value;
        for (var time = start; time < nowSeconds; time += 1.Seconds())
        {
            while (itemIdx < itemsOfSeconds.Length && 
                   itemsOfSeconds[itemIdx].Timestamp < time) itemIdx++;

            lastValue = itemIdx < itemsOfSeconds.Length && itemsOfSeconds[itemIdx].Timestamp == time
                ? itemsOfSeconds[itemIdx].Value 
                : (_aggValue ? lastValue : default);
            
            results.Add(new DataPoint<T>(time, lastValue, Window.Second));
        }
        
        itemIdx = 0;
        lastValue = itemsOfMinutes.LastOrDefault(i => i.Timestamp < start).Value;;
        for (var time = nowMinutes-60.Minutes(); time < nowMinutes; time += 1.Minutes())
        {
            while (itemIdx < itemsOfMinutes.Length && 
                   itemsOfMinutes[itemIdx].Timestamp < time) itemIdx++;

            lastValue = itemIdx < itemsOfMinutes.Length && itemsOfMinutes[itemIdx].Timestamp == time 
                ? itemsOfMinutes[itemIdx].Value 
                : (_aggValue ? lastValue : default);
            
            results.Add(new DataPoint<T>(time, lastValue, Window.Minute));
        }
        
        // foreach (var item in itemsOfSeconds)
        // {
        //     if(item.Timestamp == nowSeconds)
        //         continue;
        //     
        //     results.Add(new DataPoint<T>(item.Timestamp, item.Value, Window.Second));
        // }
        //
        // foreach (var item in itemsOfMinutes)
        // {
        //     if(item.Timestamp == nowMinutes)
        //         continue;
        //
        //     results.Add(new DataPoint<T>(item.Timestamp, item.Value, Window.Minute));
        // }

        return results;
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