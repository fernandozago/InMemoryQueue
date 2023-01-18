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
            var sutQueue = sut.GetOrCreateQueue();

            await sutQueue.EnqueueAsync(data).ConfigureAwait(false);
            var queueInfo = sutQueue.GetInfo(true);
            Assert.AreEqual(1, queueInfo.QueueSize);
            Assert.AreEqual(1, queueInfo.MainQueueSize);
            Assert.AreEqual(0, queueInfo.RetryQueueSize); 

            QueueItem? retryEmptyItem = await sutQueue.TryPeekRetryQueueAsync().ConfigureAwait(false);
            Assert.IsNull(retryEmptyItem);

            QueueItem? mainItem = await sutQueue.TryPeekMainQueueAsync().ConfigureAwait(false);
            Assert.IsNotNull(mainItem);
            Assert.AreEqual(data, mainItem.Message);
            Assert.IsFalse(mainItem.Retrying);
            Assert.AreEqual(0, mainItem.RetryCount);

            queueInfo = sutQueue.GetInfo(true);
            Assert.AreEqual(1, queueInfo.QueueSize);
            Assert.AreEqual(0, queueInfo.MainQueueSize);
            Assert.AreEqual(1, queueInfo.RetryQueueSize);

            QueueItem? retryItem = await sutQueue.TryPeekRetryQueueAsync().ConfigureAwait(false);
            Assert.IsNotNull(retryItem);
            Assert.AreEqual(data, retryItem.Message);
            Assert.IsTrue(retryItem.Retrying);
            Assert.AreEqual(1, retryItem.RetryCount);

            queueInfo = sutQueue.GetInfo(true);
            Assert.AreEqual(1, queueInfo.QueueSize);
            Assert.AreEqual(0, queueInfo.MainQueueSize);
            Assert.AreEqual(1, queueInfo.RetryQueueSize);

            retryItem = await sutQueue.TryPeekRetryQueueAsync().ConfigureAwait(false);
            Assert.IsNotNull(retryItem);
            Assert.AreEqual(data, retryItem.Message);
            Assert.IsTrue(retryItem.Retrying);
            Assert.AreEqual(2, retryItem.RetryCount);

            queueInfo = sutQueue.GetInfo(true);
            Assert.AreEqual(1, queueInfo.QueueSize);
            Assert.AreEqual(0, queueInfo.MainQueueSize);
            Assert.AreEqual(1, queueInfo.RetryQueueSize);

            queueInfo = sutQueue.GetInfo(true);
            Assert.AreEqual(0, queueInfo.Consumers.Count);
            Assert.AreEqual(0, queueInfo.ConcurrentConsumers);
        }
    }
}