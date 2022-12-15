using Microsoft.Extensions.Logging;
using Moq;

namespace MemoryQueue.Tests.SUTFactory
{
    public static class SubjectUnderTestFactory
    {
        public static InMemoryQueueManager CreateInMemoryQueueManager()
        {
            Mock<ILogger> logger = new();
            Mock<ILoggerFactory> loggerFactoryMock = new();
            Mock<ILogger<InMemoryQueueManager>> specificLogger = new();

            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

            return new(loggerFactoryMock.Object, specificLogger.Object);
        }
    }
}
