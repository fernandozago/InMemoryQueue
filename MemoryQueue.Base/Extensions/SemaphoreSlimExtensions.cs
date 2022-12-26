using System;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryQueue.Base.Extensions
{
    public struct DisposableSemaphoreSlimLock : IDisposable
    {
        private readonly SemaphoreSlim _sm;

        public DisposableSemaphoreSlimLock(SemaphoreSlim sm)
        {
            this._sm = sm;
        }

        public void Dispose()
        {
            _sm.Release();
        }
    }

    public static class SemaphoreSlimExtensions
    {
        public static async Task<IDisposable?> TryAwaitAsync(this SemaphoreSlim sm, CancellationToken token)
        {
            if (!token.IsCancellationRequested)
            {
                try
                {
                    await sm.WaitAsync(token).ConfigureAwait(false);
                    return new DisposableSemaphoreSlimLock(sm);
                }
                catch (Exception)
                {
                    //
                }
            }

            return null;
        }
    }
}
