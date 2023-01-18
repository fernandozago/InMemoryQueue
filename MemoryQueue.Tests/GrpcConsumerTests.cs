using Grpc.Core;
using MemoryQueue.Client.Grpc;
using MemoryQueue.Tests.SUTFactory;
using MemoryQueue.Base.Models;
using MemoryQueue.GRPC.Transports.GRPC.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MemoryQueue.Tests
{
    [TestClass]
    public class GrpcConsumerTests
    {
        private readonly ConsumerServiceImpl _grpcConsumer;
        private readonly SimpleGrpcServer _server;

        /// <summary>
        /// Simple Server For Testing...
        /// </summary>
        private sealed class SimpleGrpcServer : IAsyncDisposable
        {
            private readonly Server _server;

            public SimpleGrpcServer(ServerServiceDefinition serviceDefinition)
            {
                _server = new Server()
                {
                    Services = { serviceDefinition },
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
            _grpcConsumer = SubjectUnderTestFactory.CreateGrpcConsumer();
            _server = new SimpleGrpcServer(_grpcConsumer.GetServiceDefinition());
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("QueueName")]
        [DataRow("  QueueName  ")]
        public async Task AssertQueueContainsConsumers(string queueName)
        {
            using CancellationTokenSource cts = new();

            await using (_server)
            {
                try
                {
                    _server.Start();
                    var consumerClient = new GrpcQueueConsumer("127.0.0.1:12345", new NullLoggerFactory(), queueName);

                    await consumerClient.PublishAllAsync(Enumerable.Range(0, 3).Select(_ => "teste"));

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

                    int retryCount = 0;
                    while (true)
                    {
                        retryCount++;
                        var queueInfo = await consumerClient.QueueInfoAsync().ConfigureAwait(false);
                        if (queueInfo.MainQueueSize > 0 || queueInfo.AckCounter < 3 || queueInfo.PubCounter < 3)
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

                    var queueInfoReply = await consumerClient.QueueInfoAsync().ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(queueName))
                    {
                        Assert.AreEqual("Default", queueInfoReply.QueueName);
                    }
                    else
                    {
                        Assert.AreEqual(queueName.Trim(), queueInfoReply.QueueName);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                    Assert.AreEqual(3, counter);
                    Assert.AreEqual(3, queueInfoReply.Consumers.Count);
                    Assert.IsNotNull(queueInfoReply.Consumers.SingleOrDefault(x => x.Name == "Consumer1"));
                    Assert.IsNotNull(queueInfoReply.Consumers.SingleOrDefault(x => x.Name == "Consumer2"));
                    Assert.IsNotNull(queueInfoReply.Consumers.SingleOrDefault(x => x.Name == "Consumer3"));

                    Assert.AreEqual(QueueConsumerType.GRPC.ToString(), queueInfoReply.Consumers.SingleOrDefault(x => x.Name == "Consumer1")!.Type);
                    Assert.AreEqual(QueueConsumerType.GRPC.ToString(), queueInfoReply.Consumers.SingleOrDefault(x => x.Name == "Consumer2")!.Type);
                    Assert.AreEqual(QueueConsumerType.GRPC.ToString(), queueInfoReply.Consumers.SingleOrDefault(x => x.Name == "Consumer3")!.Type);

                    cts.Cancel();
                    await Task.WhenAll(consumers).ConfigureAwait(false);

                    queueInfoReply = await consumerClient.QueueInfoAsync().ConfigureAwait(false);
                    Assert.AreEqual(0, queueInfoReply.Consumers.Count);
                    Assert.IsNull(queueInfoReply.Consumers.SingleOrDefault(x => x.Name == "Consumer1"));
                    Assert.IsNull(queueInfoReply.Consumers.SingleOrDefault(x => x.Name == "Consumer2"));
                    Assert.IsNull(queueInfoReply.Consumers.SingleOrDefault(x => x.Name == "Consumer3"));
                }
                catch
                {
                    cts.Cancel();
                    throw;
                }
            }
        }
    }
}