using System;
using System.Threading;

namespace MemoryQueue.Base.Counters;

public sealed class ConsumptionConsolidator : IDisposable
{
    private static class ConsumptionConsolidatorTimer
    {
        internal static event ConsumptionConsolidatorEventHandler? OnConsolidate;
        private static readonly Timer _timer;
        private static readonly Timer _timerGc;

        static ConsumptionConsolidatorTimer()
        {
            _timer = new Timer(_ => OnConsolidate?.Invoke(), null, 1000, 1000);
            _timerGc = new Timer(GCInvoke, null, 60000, 60000);
        }

        private static void GCInvoke(object state)
        {
            Console.WriteLine("Collecting GC");
            GC.Collect();
        }
    }

    private readonly ConsumptionConsolidatorEventHandler _delegate;

    public ConsumptionConsolidator(Action consolidateAction)
    {
        _delegate = new ConsumptionConsolidatorEventHandler(consolidateAction);
        ConsumptionConsolidatorTimer.OnConsolidate += new ConsumptionConsolidatorEventHandler(consolidateAction);
    }

    public void Dispose()
    {
        ConsumptionConsolidatorTimer.OnConsolidate -= _delegate;
    }
}