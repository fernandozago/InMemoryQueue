using MemoryQueue;
using MemoryQueue.Models;
using MemoryQueue.Transports.InMemoryConsumer;

namespace GrpcService2.Services
{
    public class InMemoryDefaultQueueConsumerBackground
    {
        public InMemoryDefaultQueueConsumerBackground(IHostApplicationLifetime hostApplicationLifetime, InMemoryQueueManager inMemoryQueueManager)
        {
            _ = inMemoryQueueManager.CreateInMemoryConsumer(ProcessItem, nameof(InMemoryDefaultQueueConsumerBackground), "InMemoryConsumerQueue", hostApplicationLifetime.ApplicationStopping);
        }

        private Task<bool> ProcessItem(QueueItem arg)
        {
            return Task.FromResult(true);
        }
    }
}
