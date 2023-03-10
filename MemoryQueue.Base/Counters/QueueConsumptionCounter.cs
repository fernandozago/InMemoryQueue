using MemoryQueue.Base.Utils;

namespace MemoryQueue.Base.Counters;

public sealed class QueueConsumptionCounter : IDisposable
{
    private readonly ConsumptionConsolidator _consolidator;

    private long _ackCounter = 0;
    private long _ackPerSecond = 0;
    private long _ackPerSecond_Counter = 0;

    private long _pubCounter = 0;
    private long _pubPerSecond = 0;
    private long _pubPerSecond_Counter = 0;

    private long _redeliverCounter = 0;
    private long _redeliverPerSecond = 0;
    private long _redeliverPerSecond_Counter = 0;

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

    public long PubCounter => _pubCounter;
    public long PubPerSecond => _pubPerSecond;

    public long RedeliverCounter => _redeliverCounter;
    public long RedeliverPerSecond => _redeliverPerSecond;

    public long DeliverCounter => _deliverCounter;
    public long DeliverPerSecond => _deliverPerSecond;

    public double AvgConsumptionMs => _avgConsumptionMs;

    public QueueConsumptionCounter()
    {
        _consolidator = new ConsumptionConsolidator(Consolidate);
    }

    public void ResetCounters()
    {
        Interlocked.Exchange(ref _ackCounter, 0);
        Interlocked.Exchange(ref _ackCounter, 0);
        Interlocked.Exchange(ref _ackPerSecond, 0);
        Interlocked.Exchange(ref _ackPerSecond_Counter, 0);

        Interlocked.Exchange(ref _pubCounter, 0);
        Interlocked.Exchange(ref _pubPerSecond, 0);
        Interlocked.Exchange(ref _pubPerSecond_Counter, 0);

        Interlocked.Exchange(ref _redeliverCounter, 0);
        Interlocked.Exchange(ref _redeliverPerSecond, 0);
        Interlocked.Exchange(ref _redeliverPerSecond_Counter, 0);

        Interlocked.Exchange(ref _deliverCounter, 0);
        Interlocked.Exchange(ref _deliverPerSecond, 0);
        Interlocked.Exchange(ref _deliverPerSecond_Counter, 0);

        Interlocked.Exchange(ref _nackCounter, 0);
        Interlocked.Exchange(ref _nackPerSecond, 0);
        Interlocked.Exchange(ref _nackPerSecond_Counter, 0);

        Interlocked.Exchange(ref _avgConsumptionMs, 0);
    }

    internal void UpdateCounters(bool isRedeliver, bool isAcked, long startTimestamp)
    {
        ConsumeAvgTime(StopwatchEx.GetElapsedTime(startTimestamp).TotalMilliseconds);

        if (isRedeliver)
        {
            Redelivered();
        }
        else
        {
            Delivered();
        }

        if (isAcked)
        {
            Ack();
        }
        else
        {
            Nack();
        }
    }

    private void Ack() =>
        Interlocked.Increment(ref _ackPerSecond_Counter);

    private void Nack() =>
        Interlocked.Increment(ref _nackPerSecond_Counter);

    public void Publish() =>
        Interlocked.Increment(ref _pubPerSecond_Counter);

    private void Redelivered() =>
        Interlocked.Increment(ref _redeliverPerSecond_Counter);

    private void Delivered() =>
        Interlocked.Increment(ref _deliverPerSecond_Counter);

    private void ConsumeAvgTime(double elapsedMilliseconds) =>
        Interlocked.Exchange(ref _avgConsumptionMs, (_avgConsumptionMs + elapsedMilliseconds) / 2d);


    public void Consolidate()
    {
        Interlocked.Exchange(ref _ackPerSecond, Interlocked.Exchange(ref _ackPerSecond_Counter, 0));
        Interlocked.Add(ref _ackCounter, _ackPerSecond);

        Interlocked.Exchange(ref _pubPerSecond, Interlocked.Exchange(ref _pubPerSecond_Counter, 0));
        Interlocked.Add(ref _pubCounter, _pubPerSecond);

        Interlocked.Exchange(ref _redeliverPerSecond, Interlocked.Exchange(ref _redeliverPerSecond_Counter, 0));
        Interlocked.Add(ref _redeliverCounter, _redeliverPerSecond);

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
