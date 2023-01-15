using MemoryQueue.Client.Grpc;
using MemoryQueue.Transports.GRPC;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Trace)
        .AddConsole();
});

CancellationTokenSource cts = new ();
var queueConsumer = new GrpcQueueConsumer("127.0.0.1:1111", loggerFactory, "Default");
var consumer = queueConsumer.Consume("MyConsoleConsumer", CallBack, cts.Token);
var producer = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1));
        try
        {
            await queueConsumer.PublishAsync(new QueueItemRequest()
            {
                Message = $"teste - {DateTime.Now}"
            });
        }
        catch (Exception)
        {
            //Ignore any exception -- server may not be available
        }
    }
});

//Press any key to exit
Console.ReadKey();
cts.Cancel();
await consumer;


static Task<bool> CallBack(QueueItemReply queueItem, CancellationToken arg2)
{
    Console.WriteLine(queueItem);
    return Task.FromResult(true);
}