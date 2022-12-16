﻿using MemoryQueue.Models;
using MemoryQueue.Tests.SUTFactory;
using MemoryQueue.Transports.InMemoryConsumer;
using System.Data;

namespace MemoryQueue.Tests
{
    [TestClass]
    public class InMemoryConsumerTests
    {
        [TestMethod]
        public async Task AssertQueueContainsConsumers()
        {
            using CancellationTokenSource cts = new();

            try
            {
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

                int retryCount = 0;
                while (true)
                {
                    retryCount++;
                    if (queue.MainChannelCount > 0 || queue.Counters.AckCounter < 3 || queue.Counters.PubCounter < 3)
                    {
                        await Task.Delay(200).ConfigureAwait(false);
                    }
                    else
                    {
                        break;
                    }

                    if (retryCount > 10)
                    {
                        Assert.Fail("Fail to validate queue consumer");
                    }
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
            catch
            {
                cts.Cancel();
                throw;
            }
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

            try
            {
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
                    await queue.EnqueueAsync(data).ConfigureAwait(false);
                }

                int retryCount = 0;
                while (true)
                {
                    retryCount++;
                    if (queue.MainChannelCount > 0 || queue.Counters.AckCounter < 30 || queue.Counters.PubCounter < 30)
                    {
                        await Task.Delay(200).ConfigureAwait(false);
                    }
                    else
                    {
                        break;
                    }

                    if (retryCount > 10)
                    {
                        Assert.Fail("Fail to validate queue consumer");
                    }
                }

                cts.Cancel();
                await consumer;

                Assert.AreEqual(30, counter);
            }
            catch
            {
                cts.Cancel();
                throw;
            }
        }

        [DataTestMethod]
        [DataRow("item1")]
        [DataRow("item2")]
        [DataRow("item3")]
        public async Task AssertMessageReceivedFromRetryChannel(string data)
        {
            var manager = SubjectUnderTestFactory.CreateInMemoryQueueManager();
            var queue = manager.GetOrCreateQueue("Queue1");

            using CancellationTokenSource cts = new ();

            try
            {
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

                int retryCount = 0;
                while (true)
                {
                    retryCount++;
                    if (queue.MainChannelCount > 0 || queue.Counters.AckCounter < 30 || queue.Counters.PubCounter < 30)
                    {
                        await Task.Delay(200).ConfigureAwait(false);
                    }
                    else
                    {
                        break;
                    }

                    if (retryCount > 10)
                    {
                        Assert.Fail("Fail to validate queue consumer");
                    }
                }

                cts.Cancel();
                await consumer;

                Assert.AreEqual(30, retryCounter);
                Assert.AreEqual(60, totalCounter);
            }
            catch
            {
                cts.Cancel();
                throw;
            }
        }
    }
}