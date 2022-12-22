using MemoryQueue;
using MemoryQueue.Models;
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
            Task.FromResult((IActionResult)Ok(_inMemoryQueueManager.ActiveQueues.OrderBy(x => x.Name).Select(x => new 
            {
                x.Name,
                QueueCount = x.MainChannelCount + x.RetryChannelCount,
                x.ConsumersCount,
                x.Counters
            })));

        [HttpGet(nameof(GetQueueData))]
        public Task<IActionResult> GetQueueData(string queueName) =>
            Task.FromResult((IActionResult)Ok(_inMemoryQueueManager.GetOrCreateQueue(queueName)));

        [HttpGet(nameof(PeekMessage))]
        public Task<IActionResult?> PeekMessage(string queueName)
        {
            TryGetQueueItem(_inMemoryQueueManager.GetOrCreateQueue(queueName), out var queueItem);
            return Task.FromResult((IActionResult?)Ok(new QueueItemWrapper(queueItem)));
        }

        private bool TryGetQueueItem(IInMemoryQueue queue, out QueueItem? queueItem)
        {
            return queue.TryPeekRetryQueue(out queueItem) || queue.TryPeekMainQueue(out queueItem);
        }
        
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