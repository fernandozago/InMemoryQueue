﻿using MemoryQueue.Base;
using MemoryQueue.Models.GRPC.Services;
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
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            return new(loggerFactoryMock.Object);
        }

        public static ConsumerServiceImpl CreateGrpcConsumer()
        {
            Mock<IConfiguration> configurationMock = new();
            Mock<ILogger> loggerMock = new();
            Mock<ILoggerFactory> loggerFactoryMock = new();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            InMemoryQueueManager queueManager = new(loggerFactoryMock.Object);
            return new ConsumerServiceImpl(queueManager, configurationMock.Object, loggerFactoryMock.Object);
        }

        public static ILoggerFactory CreateLoggerFactory()
        {
            Mock<ILogger> loggerMock = new();
            Mock<ILoggerFactory> loggerFactoryMock = new();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
            return loggerFactoryMock.Object;
        }
    }
}
