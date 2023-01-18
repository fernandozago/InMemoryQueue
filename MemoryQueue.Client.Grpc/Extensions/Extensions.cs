using Grpc.Core;
using System.Runtime.CompilerServices;

namespace MemoryQueue.Client.Grpc.Extensions
{
    internal static class AsyncStreamReaderExtensions
    {
        internal static async IAsyncEnumerable<T> ReadAllAsync<T>(this IAsyncStreamReader<T> streamReader, [EnumeratorCancellation] CancellationToken token = default)
        {
            while (await streamReader.MoveNext(token).ConfigureAwait(false))
            {
                yield return streamReader.Current;
            }
        }
    }
}
