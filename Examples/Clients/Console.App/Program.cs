// See https://aka.ms/new-console-template for more information
using Grpc.Core;
using MemoryQueue.Client.Grpc;
using MemoryQueue.Transports.GRPC;
using System.Text.Json;

bool reject = false;

Console.WriteLine("Type the host and port:");
string? host = Console.ReadLine();
if (string.IsNullOrWhiteSpace(host))
{
    return;
}

Console.WriteLine("Type queue name:");
string? queueName = Console.ReadLine();

var queueConsumer = new GrpcQueueConsumer(host, queueName);

var loggerCts = new CancellationTokenSource();
loggerCts.Cancel();

var consumerCts = new CancellationTokenSource();
consumerCts.Cancel();
var consumerToken = consumerCts.Token;
Task consumer = Task.CompletedTask;


//using var t = Task.Run(async () =>
//{
//    for (var i = 0; i < 100_000; i++)
//    {
//        var result = await client.PublishAsync(new QueueItemReply()
//        {
//            Message = JsonSerializer.Serialize(new Data(Guid.Empty, DateTime.Now))
//        });
//    }
//}).ContinueWith(x => Console.WriteLine("Finish Initial Publish"));

while (Console.ReadKey().KeyChar is char c && c != 'e')
{
    if (c == 'a')
    {
        try
        {
            var call = queueConsumer.PublishAsync(new QueueItemReply()
            {
                Message = JsonSerializer.Serialize(new Data(Guid.Empty, DateTime.Now, "A"))
            });
            await call;
            Console.WriteLine(call.GetTrailers().GetValue("responseexception"));
        }
        catch(RpcException ex)
        {
            if (ex.Trailers.Get("serverexception") is Metadata.Entry serverException)
            {
                Console.WriteLine(serverException.Value);
            }
            else
            {
                Console.WriteLine(ex);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    if (c == 'b')
    {
        _ = Task.Run(async () =>
        {
            const int qtd = 100_001;
            Console.WriteLine($"Start Publishing {qtd} items...");
            var message = JsonSerializer.Serialize(new Data(Guid.Empty, DateTime.Now, "A"));
            await Parallel.ForEachAsync(Enumerable.Range(1, qtd)/*, new ParallelOptions() {  MaxDegreeOfParallelism = 1 }*/, async (a, t) =>
            {
                var result = await queueConsumer.PublishAsync(new QueueItemReply()
                {
                    Message = message
                });

                if (a % 1000 == 0)
                {
                    Console.WriteLine(a);
                }
            });
            Console.WriteLine("Publish Finished");
        });
    }

    if (c == 'l')
    {
        if (loggerCts.IsCancellationRequested)
        {
            loggerCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                while (!loggerCts.IsCancellationRequested)
                {
                    var result = await queueConsumer.QueueInfoAsync();
                    Console.WriteLine($"\nTotal Deliver: {result.DeliverCounter:N0} - {result.DeliverPerSecond:N0}/s");
                    Console.WriteLine($"Total Redeliver: {result.RedeliverCounter:N0} - {result.RedeliverPerSecond:N0}/s");
                    
                    Console.WriteLine($"Total Ack: {result.AckCounter:N0} - {result.AckPerSecond:N0}/s");
                    Console.WriteLine($"Total Nack: {result.NackCounter:N0} - {result.NackPerSecond:N0}/s");

                    Console.WriteLine($"Total Pub: {result.PubCounter:N0} - {result.PubPerSecond:N0}/s");

                    Console.WriteLine($"Avg Ack Time: {result.AvgAckTimeMilliseconds:N6} ms");                   
                    Console.WriteLine($"Queue Size: {result.QueueSize:N0} | Main Queue Size: {result.MainQueueSize:N0} | Retry Queue Size: {result.RetryQueueSize:N0}");
                    Console.WriteLine($"Active Consumers: {result.ConcurrentConsumers:N0}\n");
                    await Task.Delay(1000);
                }
            });
        }
        else
        {
            loggerCts.Cancel();
        }
    }

    if (c == 'r')
    {
        if (consumerToken.IsCancellationRequested)
        {
            consumerCts = new CancellationTokenSource();
            consumerToken = consumerCts.Token;
            consumer = queueConsumer.Consume("NewConsole Consumer", Consume, consumerToken);
        }
        else
        {
            using (consumerCts)
            {
                consumerCts.Cancel();
                await consumer;
            }
        }
    }

    if (c == 'x') 
    {
        reject = !reject;
    }
}

Task<bool> Consume(QueueItemReply item, CancellationToken token)
{
    try
    {
        //var deserialized = JsonSerializer.Deserialize<Data>(item.Message);
        //if (deserialized is null) throw new ArgumentNullException(item.Message);
        if (item.Retrying)
        {
            Console.WriteLine($"Retrying? {item.Retrying} | {item}");
        }

        return Task.FromResult(item.Retrying || !reject);
        //for (var i = item++)
        //{
        //    int z = i + 1000 * 100;
        //    z--;
        //    z++;
        //    ack = z > 0;
        //}
        //await Task.Delay(1000, consumerToken);
    }
    catch
    {
        return Task.FromResult(false);
    }
}

public record Data(Guid Info, DateTime CreateDate, string Value);