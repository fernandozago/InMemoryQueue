using System.Threading;
using System.Threading.Tasks;

namespace MemoryQueue.Base.Utils
{
    public static class TaskEx
    {

        public static async Task SafeDelay(int millisecondsDelay, CancellationToken token = default)
        {
            if (!token.CanBeCanceled)
            {
                await Task.Delay(millisecondsDelay, token).ConfigureAwait(false);
                return;
            }

            try
            {
                await Task.Delay(millisecondsDelay, token).ConfigureAwait(false);
            }
            catch
            {
                //
            }
        }

    }
}
