using MemoryQueue.Tests.SUTFactory;

namespace MemoryQueue.Tests
{
    [TestClass]
    public class InMemoryQueueManagerTests
    {
        [TestMethod]
        public void AssertGetOrCreateQueue_DefaultName()
        {
            var sut = SubjectUnderTestFactory.CreateInMemoryQueueManager();

            Assert.AreEqual(0, sut.ActiveQueues.Count);

            var queue = sut.GetOrCreateQueue(null);
            Assert.AreEqual("Default", queue.GetInfo().QueueName);

            queue = sut.GetOrCreateQueue("");
            Assert.AreEqual("Default", queue.GetInfo().QueueName);

            Assert.AreEqual(1, sut.ActiveQueues.Count);
        }

        [DataTestMethod]
        [DataRow("Custom1")]
        [DataRow("Custom2")]
        [DataRow("1Custom1")]
        public void AssertGetOrCreateQueue_CustomNaame(string queueName)
        {
            var sut = SubjectUnderTestFactory.CreateInMemoryQueueManager();

            Assert.AreEqual(0, sut.ActiveQueues.Count);

            var queue = sut.GetOrCreateQueue(queueName);
            Assert.AreEqual(queueName, queue.GetInfo().QueueName);

            queue = sut.GetOrCreateQueue(queueName);
            Assert.AreEqual(queueName, queue.GetInfo().QueueName);

            Assert.AreEqual(1, sut.ActiveQueues.Count);
        }

        [TestMethod]
        public void AssertThrowExceptionInvalidQueueName()
        {
            var sut = SubjectUnderTestFactory.CreateInMemoryQueueManager();

            Assert.AreEqual(0, sut.ActiveQueues.Count);
            Assert.ThrowsException<InvalidOperationException>(() => sut.GetOrCreateQueue("!@#!#@"), "Failed parsing queuename !@#!#@");
            Assert.AreEqual(0, sut.ActiveQueues.Count);
        }
    }
}