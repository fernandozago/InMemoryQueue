using Grpc.Core;
using System.Runtime.CompilerServices;

namespace GrpcClient3
{
    public static class AsyncStreamReaderExtensions
    {
        public static async IAsyncEnumerable<T> ReadAllAsync<T>(this IAsyncStreamReader<T> streamReader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (await streamReader.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                yield return streamReader.Current;
            }
        }
    }
}
