// See https://aka.ms/new-console-template for more information
using MemoryQueue.Client.SignalR;
using MemoryQueue.Transports.SignalR;

Console.WriteLine("Hello, World!");
InMemoryQueueSignalrClient inMemoryQueueSignalr = new InMemoryQueueSignalrClient("https://localhost:7134/inmemoryqueue/hub");

await Task.Delay(5000);
await inMemoryQueueSignalr.PublishAsync("TESTE1");
await inMemoryQueueSignalr.PublishAsync("TESTE2");
await inMemoryQueueSignalr.PublishAsync("TESTE3");
await inMemoryQueueSignalr.PublishAsync("TESTE4");
await inMemoryQueueSignalr.PublishAsync("TESTE5");

var consumer = inMemoryQueueSignalr.ConsumeAsync("Consumer", ConsumeMessage, CancellationToken.None);

Console.ReadKey();

static Task<bool> ConsumeMessage(QueueItemReply item, CancellationToken arg2)
{
    Console.WriteLine(item.Message);
    return Task.FromResult(true);
}