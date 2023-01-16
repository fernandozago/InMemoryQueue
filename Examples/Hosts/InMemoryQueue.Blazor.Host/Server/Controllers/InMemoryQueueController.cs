using MemoryQueue.Base;
using MemoryQueue.Base.Models;
using Microsoft.AspNetCore.Mvc;

namespace InMemoryQueue.Blazor_Host.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InMemoryQueueController : ControllerBase
    {
        private readonly IInMemoryQueueManager _inMemoryQueueManager;
        private readonly ILogger<InMemoryQueueController> _logger;

        public InMemoryQueueController(IInMemoryQueueManager inMemoryQueueManager, ILogger<InMemoryQueueController> logger)
        {
            _inMemoryQueueManager = inMemoryQueueManager;
            _logger = logger;
        }

        [HttpGet(nameof(GetActiveQueues))]
        public Task<IActionResult> GetActiveQueues() =>
            Task.FromResult((IActionResult)Ok(_inMemoryQueueManager.ActiveQueues.Select(x => x.Value.GetInfo())));

        [HttpGet(nameof(GetQueueData))]
        public Task<IActionResult> GetQueueData(string queueName) =>
            Task.FromResult((IActionResult)Ok(_inMemoryQueueManager.GetOrCreateQueue(queueName)));

        [HttpGet(nameof(PeekMessage))]
        public async Task<IActionResult?> PeekMessage(string queueName) =>
            Ok(new QueueItemWrapper(await TryGetQueueItem(_inMemoryQueueManager.GetOrCreateQueue(queueName)).ConfigureAwait(false)));

        private static async Task<QueueItem?> TryGetQueueItem(IInMemoryQueue queue) =>
            (await queue.TryPeekRetryQueueAsync().ConfigureAwait(false)) ?? (await queue.TryPeekMainQueueAsync().ConfigureAwait(false));

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