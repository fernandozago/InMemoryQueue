using MemoryQueue.Base;
using MemoryQueue.GRPC.Transports.GRPC.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MemoryQueue.Tests.SUTFactory
{
    public static class SubjectUnderTestFactory
    {
        public static InMemoryQueueManager CreateInMemoryQueueManager() =>
            new(new NullLoggerFactory());

        public static ConsumerServiceImpl CreateGrpcConsumer() =>
            new (CreateInMemoryQueueManager(), new Mock<IConfiguration>().Object, new NullLoggerFactory());
    }
}
