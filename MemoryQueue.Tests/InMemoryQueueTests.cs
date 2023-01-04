using MemoryQueue.Tests.SUTFactory;

namespace MemoryQueue.Tests
{
    [TestClass]
    public class InMemoryQueueTests
    {
        [DataTestMethod]
        [DataRow("item1")]
        [DataRow("item2")]
        [DataRow("item3")]
        public async Task AssertCanEnqueueItem(string data)
        {
            var sut = SubjectUnderTestFactory.CreateInMemoryQueueManager();
            var queue = sut.GetOrCreateQueue(string.Empty);

            await queue.EnqueueAsync(data).ConfigureAwait(false);

            Assert.AreEqual(1, queue.MainChannelCount);
            Assert.AreEqual(0, queue.RetryChannelCount);

            Assert.IsTrue(queue.TryPeekMainQueue(out var itemMain));
            Assert.AreEqual(data, itemMain!.Message);
            Assert.IsFalse(itemMain!.Retrying);
            Assert.AreEqual(0, itemMain!.RetryCount);

            Assert.IsTrue(queue.TryPeekRetryQueue(out var itemRetry));
            Assert.AreEqual(data, itemRetry!.Message);
            Assert.IsTrue(itemRetry!.Retrying);
            Assert.AreEqual(1, itemRetry!.RetryCount);

            Assert.AreEqual(0, queue.Consumers.Count);
            Assert.AreEqual(0, queue.ConsumersCount);
        }
    }
}