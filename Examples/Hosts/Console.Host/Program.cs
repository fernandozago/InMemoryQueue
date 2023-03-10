using MemoryQueue.Base;
using MemoryQueue.Base.InMemoryConsumer;
using MemoryQueue.Base.Models;
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
var consumer = InMemoryQueueManager.CreateInMemoryConsumer(Consume, token: cts.Token);

Console.ReadKey();
cts.Cancel();
await consumer;

Task<bool> Consume(QueueItem item, CancellationToken token)
{
    _logger.LogInformation("Message Received: {message} at {dateTime}", item.Message, DateTime.Now);
    return Task.FromResult(true);
}