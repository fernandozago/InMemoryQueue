using System.Collections.Concurrent;

namespace MemoryQueue.Base.Extensions;

internal static class QueueNameHashesGenerator
{
    private static readonly ConcurrentDictionary<string, Lazy<int>> _cachedHashes = new();

    /// <summary>
    /// Generates (or get cached) a stable hashcode from a string
    /// </summary>
    /// <param name="queueName"></param>
    /// <returns></returns>
    internal static int GenerateHash(string queueName) =>
        _cachedHashes.GetOrAdd(queueName.ToUpper(), new Lazy<int>(() => GetHash(queueName.ToUpper()))).Value;

    private static int GetHash(string str)
    {
        unchecked
        {
            int hash1 = 5381;
            int hash2 = hash1;

            for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
            {
                hash1 = (hash1 << 5) + hash1 ^ str[i];
                if (i == str.Length - 1 || str[i + 1] == '\0')
                    break;
                hash2 = (hash2 << 5) + hash2 ^ str[i + 1];
            }

            int result = hash1 + hash2 * 1566083941;
            return result;
        }
    }
}
