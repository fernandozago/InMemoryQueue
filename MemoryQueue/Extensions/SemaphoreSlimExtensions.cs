namespace MemoryQueue.Extensions;

internal static class SemaphoreSlimExtensions
{
    internal record SemaphoreSlimDisposableLocker : IDisposable
    {
        private readonly SemaphoreSlim _sm;

        internal SemaphoreSlimDisposableLocker(SemaphoreSlim sm) =>
            _sm = sm;

        public void Dispose() =>
            _sm.Release();
    }

    internal static async Task<IDisposable?> TryWaitAsync(this SemaphoreSlim sm, CancellationToken token)
    {
        try
        {
            if (token.IsCancellationRequested)
            {
                return default;
            }

            await sm.WaitAsync(token).ConfigureAwait(false);
            return new SemaphoreSlimDisposableLocker(sm);
        }
        catch (Exception)
        {
            return default;
        }
    }
}

