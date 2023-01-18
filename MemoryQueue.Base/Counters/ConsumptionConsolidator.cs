namespace MemoryQueue.Base.Counters;

public sealed class ConsumptionConsolidator : IDisposable
{
    private static class ConsumptionConsolidatorTimer
    {
        internal static event ConsumptionConsolidatorEventHandler? OnConsolidate;
        private static readonly Timer _timer;

        static ConsumptionConsolidatorTimer()
        {
            _timer = new Timer(_ => OnConsolidate?.Invoke(), null, 1000, 1000);
        }
    }

    private readonly ConsumptionConsolidatorEventHandler _delegate;

    public ConsumptionConsolidator(Action consolidateAction)
    {
        _delegate = new ConsumptionConsolidatorEventHandler(consolidateAction);
        ConsumptionConsolidatorTimer.OnConsolidate += _delegate;
    }

    public void Dispose()
    {
        ConsumptionConsolidatorTimer.OnConsolidate -= _delegate;
    }
}