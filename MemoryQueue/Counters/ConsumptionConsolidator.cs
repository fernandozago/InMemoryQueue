using System.Diagnostics;

namespace MemoryQueue.Counters;

public abstract class ConsumptionConsolidator : IDisposable
{
    private static readonly Task? _consolidatorTask;
    private static readonly object _locker = new();
    protected static event ConsumptionConsolidatorEventHandler? OnConsolidate;

    static ConsumptionConsolidator()
    {
        //Avoid race conditions when initializing app (Multiple queues/consumers can be created at the same time)
        lock (_locker)
        {
            if (_consolidatorTask is null)
            {
                _consolidatorTask = ConsolidatorTask();
            }
        }
    }

    private static async Task ConsolidatorTask()
    {
        using var pd = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await pd.WaitForNextTickAsync(CancellationToken.None).ConfigureAwait(false))
            {
                //var stamp = Stopwatch.GetTimestamp();
                OnConsolidate?.Invoke();
                //Console.WriteLine(Stopwatch.GetElapsedTime(stamp).TotalMilliseconds);
            }
        }
        catch (Exception)
        {
            //
        }
    }

    /// <summary>
    /// Should implement Dispose and remove the handler added on OnConsolidate Event
    /// </summary>
    public abstract void Dispose();
}
