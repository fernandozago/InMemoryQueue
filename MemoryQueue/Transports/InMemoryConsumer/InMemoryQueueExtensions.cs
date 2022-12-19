using MemoryQueue.Models;
using Microsoft.Extensions.Logging;

namespace MemoryQueue.Transports.InMemoryConsumer
{
    public static class InMemoryQueueExtensions
    {
        private const string IN_MEMORY_CONSUMER_NA = "N/A";

        public static async Task CreateInMemoryConsumer(this IInMemoryQueue inMemoryQueue,
            Func<QueueItem, Task<bool>> callBack, string? consumerName = null, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid().ToString();
            await using var reader = ((InMemoryQueue)inMemoryQueue).AddQueueReader(new QueueConsumer(QueueConsumerType.InMemory)
            {
                Id = id,
                Name = consumerName ?? id,
                Host = IN_MEMORY_CONSUMER_NA,
                Ip = IN_MEMORY_CONSUMER_NA,
            }, callBack, cancellationToken);

            await reader.Completed.Task.ConfigureAwait(false);
            ((InMemoryQueue)inMemoryQueue).RemoveReader(reader);
        }

        public static Task CreateInMemoryConsumer(this InMemoryQueueManager inMemoryQueueManager,
            Func<QueueItem, Task<bool>> callBack, string? consumerName = null, string? queueName = null, CancellationToken cancellationToken = default) =>
                inMemoryQueueManager.GetOrCreateQueue(queueName).CreateInMemoryConsumer(callBack, consumerName, cancellationToken);
    }
}
