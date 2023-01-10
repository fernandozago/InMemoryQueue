using MemoryQueue.Base.Models;
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


            QueueItem? retryEmptyItem = await queue.TryPeekRetryQueue().ConfigureAwait(false);
            Assert.IsNull(retryEmptyItem);

            QueueItem? mainItem = await queue.TryPeekMainQueue().ConfigureAwait(false);
            Assert.IsNotNull(mainItem);
            Assert.AreEqual(data, mainItem.Message);
            Assert.IsFalse(mainItem.Retrying);
            Assert.AreEqual(0, mainItem.RetryCount);

            QueueItem? retryItem = await queue.TryPeekRetryQueue().ConfigureAwait(false);
            Assert.IsNotNull(retryItem);
            Assert.AreEqual(data, retryItem.Message);
            Assert.IsTrue(retryItem.Retrying);
            Assert.AreEqual(1, retryItem.RetryCount);

            Assert.AreEqual(0, queue.Consumers.Count);
            Assert.AreEqual(0, queue.ConsumersCount);
        }
    }
}