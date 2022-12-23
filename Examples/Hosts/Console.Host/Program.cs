using MemoryQueue;
using MemoryQueue.Models;
using MemoryQueue.Models.InMemoryConsumer;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Trace)
    .AddConsole();
});

var _logger = loggerFactory.CreateLogger<Program>();

InMemoryQueueManager InMemoryQueueManager = new(loggerFactory);
_ = Task.Run(async () =>
{
    var queue = InMemoryQueueManager.GetOrCreateQueue();

    for (var i = 0; i < 100; i++)
    {
        await queue.EnqueueAsync($"test {DateTime.Now}");
    }

    while (true)
    {
        await queue.EnqueueAsync($"test {DateTime.Now}");
        await Task.Delay(500);
    }
});

using CancellationTokenSource cts = new ();
var consumer = InMemoryQueueManager.CreateInMemoryConsumer(Consume, cancellationToken: cts.Token);

Console.ReadKey();
cts.Cancel();
await consumer;

Task<bool> Consume(QueueItem item)
{
    _logger.LogInformation("Message Received: {message} at {dateTime}", item.Message, DateTime.Now);
    return Task.FromResult(true);
}