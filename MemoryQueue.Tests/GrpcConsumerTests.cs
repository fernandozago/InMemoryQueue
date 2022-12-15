using Grpc.Core;
using MemoryQueue.Client.Grpc;
using MemoryQueue.Models;
using MemoryQueue.Tests.SUTFactory;
using MemoryQueue.Transports.GRPC.Services;

namespace MemoryQueue.Tests
{
    [TestClass]
    public class GrpcConsumerTests
    {
        private readonly ConsumerServiceImpl _grpcConsumer;
        private readonly SimpleGrpcServer _server;
        private readonly InMemoryQueueManager _inMemoryQueueManager;

        /// <summary>
        /// Simple Server For Testing...
        /// </summary>
        private sealed class SimpleGrpcServer : IAsyncDisposable
        {
            private readonly Server _server;

            public SimpleGrpcServer(ConsumerServiceImpl service)
            {
                _server = new Server()
                {
                    Services = { ConsumerServiceImpl.Bind(service) },
                    Ports = { new ServerPort("127.0.0.1", 12345, ServerCredentials.Insecure) }
                };
            }

            public void Start()
            {
                _server.Start();
            }

            public async ValueTask DisposeAsync()
            {
                await _server.ShutdownAsync();
            }
        }

        public GrpcConsumerTests()
        {
            (_grpcConsumer, _inMemoryQueueManager) = SubjectUnderTestFactory.CreateGrpcConsumer();
            _server = new SimpleGrpcServer(_grpcConsumer);
        }

        [TestMethod]
        public async Task AssertQueueContainsConsumers()
        {
            await using (_server)
            {
                using CancellationTokenSource cts = new();
                _server.Start();

                var consumerClient = new GrpcQueueConsumer("127.0.0.1:12345", "TestQueue1");
                int counter = 0;
                var consumers = new List<Task>()
            {
                consumerClient.Consume("Consumer1", (item, token) =>
                {
                    if (item.Message.Equals("teste"))
                    {
                        Interlocked.Increment(ref counter);
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                }, cts.Token),
                consumerClient.Consume("Consumer2", (item, token) =>
                {
                    if (item.Message.Equals("teste"))
                    {
                        Interlocked.Increment(ref counter);
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                }, cts.Token),
                consumerClient.Consume("Consumer3", (item, token) =>
                {
                    if (item.Message.Equals("teste"))
                    {
                        Interlocked.Increment(ref counter);
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                }, cts.Token)
            };

                var queue = _inMemoryQueueManager.GetOrCreateQueue("TestQueue1");
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

                Assert.AreEqual(QueueConsumerType.GRPC, queue.Consumers.SingleOrDefault(x => x.Name == "Consumer1")!.ConsumerType);
                Assert.AreEqual(QueueConsumerType.GRPC, queue.Consumers.SingleOrDefault(x => x.Name == "Consumer2")!.ConsumerType);
                Assert.AreEqual(QueueConsumerType.GRPC, queue.Consumers.SingleOrDefault(x => x.Name == "Consumer3")!.ConsumerType);

                cts.Cancel();
                await Task.WhenAll(consumers).ConfigureAwait(false);

                Assert.AreEqual(0, queue.Consumers.Count);
                Assert.IsNull(queue.Consumers.SingleOrDefault(x => x.Name == "Consumer1"));
                Assert.IsNull(queue.Consumers.SingleOrDefault(x => x.Name == "Consumer2"));
                Assert.IsNull(queue.Consumers.SingleOrDefault(x => x.Name == "Consumer3"));
            }
        }
    }
}