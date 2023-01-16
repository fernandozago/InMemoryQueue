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

            await Task.Delay(1000);
            Assert.AreEqual(1, queue.GetInfo().MainQueueSize);
            Assert.AreEqual(0, queue.GetInfo().RetryQueueSize);


            QueueItem? retryEmptyItem = await queue.TryPeekRetryQueueAsync().ConfigureAwait(false);
            Assert.IsNull(retryEmptyItem);

            QueueItem? mainItem = await queue.TryPeekMainQueueAsync().ConfigureAwait(false);
            Assert.IsNotNull(mainItem);
            Assert.AreEqual(data, mainItem.Message);
            Assert.IsFalse(mainItem.Retrying);
            Assert.AreEqual(0, mainItem.RetryCount);

            QueueItem? retryItem = await queue.TryPeekRetryQueueAsync().ConfigureAwait(false);
            Assert.IsNotNull(retryItem);
            Assert.AreEqual(data, retryItem.Message);
            Assert.IsTrue(retryItem.Retrying);
            Assert.AreEqual(1, retryItem.RetryCount);

            Assert.AreEqual(0, queue.GetInfo().Consumers.Count);
            Assert.AreEqual(0, queue.GetInfo().ConcurrentConsumers);
        }
    }
}