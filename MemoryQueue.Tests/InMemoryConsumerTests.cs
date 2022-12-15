using MemoryQueue.Models;
using MemoryQueue.Tests.SUTFactory;
using MemoryQueue.Transports.InMemoryConsumer;

namespace MemoryQueue.Tests
{
    [TestClass]
    public class InMemoryConsumerTests
    {
        [TestMethod]
        public async Task AssertQueueContainsConsumers()
        {
            using CancellationTokenSource cts = new();

            var queue = SubjectUnderTestFactory
                .CreateInMemoryQueueManager()
                .GetOrCreateQueue("Queue1");

            int counter = 0;
            var consumers = new List<Task>()
            {
                queue.CreateInMemoryConsumer((item) =>
                {
                    if (item.Message.Equals("teste"))
                    {
                        Interlocked.Increment(ref counter);
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                }, "Consumer1", cts.Token),

                queue.CreateInMemoryConsumer((item) =>
                {
                    if (item.Message.Equals("teste"))
                    {
                        Interlocked.Increment(ref counter);
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                }, "Consumer2", cts.Token),

                queue.CreateInMemoryConsumer((item) =>
                {
                    if (item.Message.Equals("teste"))
                    {
                        Interlocked.Increment(ref counter);
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                }, "Consumer3", cts.Token)
            };


            await queue.EnqueueAsync("teste").ConfigureAwait(false);
            await queue.EnqueueAsync("teste").ConfigureAwait(false);
            await queue.EnqueueAsync("teste").ConfigureAwait(false);
            while (queue.MainChannelCount > 0)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }

            Assert.AreEqual(3, counter);
            Assert.AreEqual(3, queue.Consumers.Count);
            Assert.IsNotNull(queue.Consumers.SingleOrDefault(x => x.Name == "Consumer1"));
            Assert.IsNotNull(queue.Consumers.SingleOrDefault(x => x.Name == "Consumer2"));
            Assert.IsNotNull(queue.Consumers.SingleOrDefault(x => x.Name == "Consumer3"));

            Assert.AreEqual(QueueConsumerType.InMemory, queue.Consumers.SingleOrDefault(x => x.Name == "Consumer1")!.ConsumerType);
            Assert.AreEqual(QueueConsumerType.InMemory, queue.Consumers.SingleOrDefault(x => x.Name == "Consumer2")!.ConsumerType);
            Assert.AreEqual(QueueConsumerType.InMemory, queue.Consumers.SingleOrDefault(x => x.Name == "Consumer3")!.ConsumerType);

            cts.Cancel();
            await Task.WhenAll(consumers);

            Assert.AreEqual(0, queue.Consumers.Count);
            Assert.IsNull(queue.Consumers.SingleOrDefault(x => x.Name == "Consumer1"));
            Assert.IsNull(queue.Consumers.SingleOrDefault(x => x.Name == "Consumer2"));
            Assert.IsNull(queue.Consumers.SingleOrDefault(x => x.Name == "Consumer3"));
        }

        [DataTestMethod]
        [DataRow("item1")]
        [DataRow("item2")]
        [DataRow("item3")]
        public async Task AssertMessageReceivedFromMainChannel(string data)
        {
            var queue = SubjectUnderTestFactory
                .CreateInMemoryQueueManager()
                .GetOrCreateQueue("Queue1");

            using CancellationTokenSource cts = new ();

            int counter = 0;
            var consumer = queue.CreateInMemoryConsumer((item) =>
            {
                if (item.Message.Equals(data))
                {
                    counter++;
                }               
                return Task.FromResult(true);
            }, "Consumer1", cts.Token);

            for (int i = 0; i < 30; i++)
            {
                await queue.EnqueueAsync(data); 
            }

            while (queue.MainChannelCount > 0)
            {
                await Task.Delay(100);
            }

            cts.Cancel();
            await consumer;

            Assert.AreEqual(30, counter);
        }

        [DataTestMethod]
        [DataRow("item1")]
        [DataRow("item2")]
        [DataRow("item3")]
        public async Task AssertMessageReceivedFromRetryChannel(string data)
        {
            var manager = SubjectUnderTestFactory.CreateInMemoryQueueManager();
            var queue = manager.GetOrCreateQueue("Queue1");

            using CancellationTokenSource cts = new CancellationTokenSource();

            int totalCounter = 0;
            int retryCounter = 0;
            var consumer = manager.CreateInMemoryConsumer((item) =>
            {
                totalCounter++;
                if (!item.Retrying)
                {
                    return Task.FromResult(false);
                }

                if (item.Message.Equals(data))
                {
                    retryCounter++;
                }                
                return Task.FromResult(true);
            }, "Consumer1", "Queue1", cts.Token);

            for (int i = 0; i < 30; i++)
            {
                await queue.EnqueueAsync(data);
            }

            while (queue.MainChannelCount > 0)
            {
                await Task.Delay(100);
            }

            cts.Cancel();
            await consumer;

            Assert.AreEqual(30, retryCounter);
            Assert.AreEqual(60, totalCounter);
        }
    }
}