using MemoryQueue.Base;
using MemoryQueue.GRPC.Transports.GRPC.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MemoryQueue.Tests.SUTFactory
{
    public static class SubjectUnderTestFactory
    {
        public static InMemoryQueueManager CreateInMemoryQueueManager()
        {
            return new(new NullLoggerFactory());
        }

        public static ConsumerServiceImpl CreateGrpcConsumer()
        {
            var loggerFactory = new NullLoggerFactory();
            Mock<IConfiguration> configurationMock = new();
            InMemoryQueueManager queueManager = new(loggerFactory);
            return new ConsumerServiceImpl(queueManager, configurationMock.Object, loggerFactory);
        }
    }
}
