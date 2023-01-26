using MemoryQueue.Base.Models;
using MemoryQueue.Tests.SUTFactory;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using System.Transactions;

namespace MemoryQueue.Tests
{
    [TestClass]
    public class InMemoryQueueStorageTests
    {
        private bool ProcessItem()
        {
            return Random.Shared.Next(0, 2) == 0;
        }

        [DataTestMethod]
        [DataRow("item1")]
        public async Task AssertStorage(string data)
        {            
            var inMemoryQueueStore = SubjectUnderTestFactory.CreateInMemoryQueueStore("Default");
            int refUpsertCount = 0;
            int refDeleteCount = 0;

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(45));
            TransformBlock<QueueItem, QueueItem> transformBlock = new TransformBlock<QueueItem, QueueItem>(inMemoryQueueStore.UpsertAsync);
            ActionBlock<QueueItem>? deleteBlock = null;
            var DeleteItem = new Action<QueueItem>(async (item) =>
            {
                try
                {
                    await Task.Delay(Random.Shared.Next(10, 500));
                    if (cts.IsCancellationRequested || ProcessItem() || !await transformBlock.SendAsync(item.Retry()))
                    {
                        await inMemoryQueueStore.DeleteAsync(item);
                        Interlocked.Increment(ref refDeleteCount);
                    }

                    if (cts.IsCancellationRequested)
                    {
                        transformBlock.Complete();
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                }
            });
            deleteBlock = new ActionBlock<QueueItem>(DeleteItem, new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = 100,
                BoundedCapacity = 1
            });

            transformBlock.LinkTo(deleteBlock, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });

            await Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(1);
                    for (var i = 0; i < 5; i++)
                    {
                        if (await transformBlock.SendAsync(new QueueItem(i.ToString())))
                        {
                            Interlocked.Increment(ref refUpsertCount);
                        }
                    }
                }
            });
            Trace.TraceInformation($"Publish Task Completed");
            await deleteBlock.Completion;

            while (refUpsertCount != refDeleteCount)
            {
                Trace.TraceInformation($"Still Different {refUpsertCount}!={refDeleteCount} -- {transformBlock.InputCount} -- {transformBlock.OutputCount} -- {deleteBlock.InputCount}");
                await Task.Delay(1000);
            }

            Trace.TraceInformation($"Check {refUpsertCount}=={refDeleteCount} -- {transformBlock.InputCount} -- {transformBlock.OutputCount} -- {deleteBlock.InputCount}");
            Assert.AreEqual(refUpsertCount, refDeleteCount);

            while (inMemoryQueueStore._balance > 0)
            {
                Trace.TraceInformation($"Check {inMemoryQueueStore._balance}!={0}");
                await Task.Delay(1000);
            }

            await Task.Delay(10000);
            Assert.AreEqual(0, inMemoryQueueStore._balance);
        }
    }
}
