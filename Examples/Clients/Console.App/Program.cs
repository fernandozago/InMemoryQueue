using MemoryQueue.Client.Grpc;
using MemoryQueue.Transports.GRPC;

CancellationTokenSource cts = new CancellationTokenSource();
var queueConsumer = new GrpcQueueConsumer("127.0.0.1:5000");
var consumer = queueConsumer.Consume("MyConsoleConsumer", CallBack, cts.Token);
var producer = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        await queueConsumer.PublishAsync(new QueueItemReply()
        {
            Message = "teste"
        });
    }
});

//Press any key to exit
Console.ReadKey();
cts.Cancel();
await consumer;


Task<bool> CallBack(QueueItemReply queueItem, CancellationToken arg2)
{
    Console.WriteLine(queueItem);
    return Task.FromResult(true);
}