using MemoryQueue.Base.Utils;

namespace MemoryQueue.Base.Counters;

public sealed record ReaderConsumptionCounter : IDisposable
{
    private readonly QueueConsumptionCounter _queueCounter;
    private readonly ConsumptionConsolidator _consolidator;

    private long _ackCounter = 0;
    private long _ackPerSecond = 0;
    private long _ackPerSecond_Counter = 0;

    private long _deliverCounter;
    private long _deliverPerSecond = 0;
    private long _deliverPerSecond_Counter = 0;

    private long _nackCounter = 0;
    private long _nackPerSecond = 0;
    private long _nackPerSecond_Counter = 0;

    private double _avgConsumptionMs = 0;

    public long AckCounter => _ackCounter;
    public long AckPerSecond => _ackPerSecond;

    public long NackCounter => _nackCounter;
    public long NackPerSecond => _nackPerSecond;

    public long DeliverCounter => _deliverCounter;
    public long DeliverPerSecond => _deliverPerSecond;

    public double AvgConsumptionMs => _avgConsumptionMs;

    public bool Throttled { get; private set; }

    public ReaderConsumptionCounter(QueueConsumptionCounter queueCounter)
    {
        _queueCounter = queueCounter;
        _consolidator = new ConsumptionConsolidator(Consolidate);
    }

    public void SetThrottled(bool throttled)
    {
        if (Throttled != throttled)
        {
            Throttled = throttled;
        }
    }

    public void UpdateCounters(bool isRedeliver, bool isAcked, long startTimestamp)
    {
        ConsumeAvgTime(StopwatchEx.GetElapsedTime(startTimestamp).TotalMilliseconds);

        Delivered();

        if (isAcked)
        {
            Ack();
        }
        else
        {
            Nack();
        }

        _queueCounter.UpdateCounters(isRedeliver, isAcked, startTimestamp);
    }

    private void Ack() =>
        Interlocked.Increment(ref _ackPerSecond_Counter);

    private void Nack() =>
        Interlocked.Increment(ref _nackPerSecond_Counter);

    private void Delivered() =>
        Interlocked.Increment(ref _deliverPerSecond_Counter);

    private void ConsumeAvgTime(double elapsedMilliseconds) =>
        Interlocked.Exchange(ref _avgConsumptionMs, (_avgConsumptionMs + elapsedMilliseconds) / 2d);

    public void Consolidate()
    {
        Interlocked.Exchange(ref _ackPerSecond, Interlocked.Exchange(ref _ackPerSecond_Counter, 0));
        Interlocked.Add(ref _ackCounter, _ackPerSecond);

        Interlocked.Exchange(ref _deliverPerSecond, Interlocked.Exchange(ref _deliverPerSecond_Counter, 0));
        Interlocked.Add(ref _deliverCounter, _deliverPerSecond);

        Interlocked.Exchange(ref _nackPerSecond, Interlocked.Exchange(ref _nackPerSecond_Counter, 0));
        Interlocked.Add(ref _nackCounter, _nackPerSecond);
    }

    public void Dispose()
    {
        _consolidator.Dispose();
    }
}
