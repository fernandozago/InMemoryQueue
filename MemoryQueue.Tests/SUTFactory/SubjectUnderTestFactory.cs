using MemoryQueue.Transports.GRPC.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MemoryQueue.Tests.SUTFactory
{
    public static class SubjectUnderTestFactory
    {
        public static InMemoryQueueManager CreateInMemoryQueueManager()
        {

            Mock<ILogger> loggerMock = new();
            Mock<ILoggerFactory> loggerFactoryMock = new();
            Mock<ILogger<InMemoryQueueManager>> specificLoggerMock = new();

            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            return new(loggerFactoryMock.Object, specificLoggerMock.Object);
        }

        public static ConsumerServiceImpl CreateGrpcConsumer()
        {
            Mock<IConfiguration> configurationMock = new();
            Mock<ILogger> loggerMock = new();
            Mock<ILoggerFactory> loggerFactoryMock = new();
            Mock<ILogger<InMemoryQueueManager>> specificLoggerMock = new();

            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            InMemoryQueueManager queueManager = new(loggerFactoryMock.Object, specificLoggerMock.Object);

            return new ConsumerServiceImpl(queueManager, configurationMock.Object, loggerFactoryMock.Object);
        }
    }
}
