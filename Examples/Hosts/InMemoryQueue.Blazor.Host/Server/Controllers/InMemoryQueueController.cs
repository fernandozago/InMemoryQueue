using MemoryQueue.Base;
using MemoryQueue.Base.Models;
using Microsoft.AspNetCore.Mvc;

namespace InMemoryQueue.Blazor_Host.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InMemoryQueueController : ControllerBase
    {
        private readonly InMemoryQueueManager _inMemoryQueueManager;
        private readonly ILogger<InMemoryQueueController> _logger;

        public InMemoryQueueController(InMemoryQueueManager inMemoryQueueManager, ILogger<InMemoryQueueController> logger)
        {
            this._inMemoryQueueManager = inMemoryQueueManager;
            _logger = logger;
        }

        [HttpGet(nameof(GetActiveQueues))]
        public Task<IActionResult> GetActiveQueues() =>
            Task.FromResult((IActionResult)Ok(_inMemoryQueueManager.ActiveQueues.OrderBy(x => x.Value.Name).Select(x => new
            {
                x.Value.Name,
                QueueCount = x.Value.MainChannelCount + x.Value.RetryChannelCount,
                x.Value.ConsumersCount,
                x.Value.Counters
            })));

        [HttpGet(nameof(GetQueueData))]
        public Task<IActionResult> GetQueueData(string queueName) =>
            Task.FromResult((IActionResult)Ok(_inMemoryQueueManager.GetOrCreateQueue(queueName)));

        [HttpGet(nameof(PeekMessage))]
        public async Task<IActionResult?> PeekMessage(string queueName) =>
            Ok(new QueueItemWrapper(await TryGetQueueItem(_inMemoryQueueManager.GetOrCreateQueue(queueName)).ConfigureAwait(false)));

        private async Task<QueueItem?> TryGetQueueItem(IInMemoryQueue queue) =>
            (await queue.TryPeekRetryQueue().ConfigureAwait(false)) ?? (await queue.TryPeekMainQueue().ConfigureAwait(false));

        private class QueueItemWrapper
        {
            public QueueItemWrapper(QueueItem? item = null)
            {
                Item = item;
            }

            public QueueItem? Item { get; }
        }
    }
}