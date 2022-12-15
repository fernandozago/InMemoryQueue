# InMemoryQueue
Creates a in memory queue service in .net



# How to use it ? (Server)

- Server using `Kestrel`:
- Add package: InMemoryQueue

`Program.cs`


```csharp
using MemoryQueue;
using MemoryQueue.Transports.GRPC.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<InMemoryQueueManager>(); //<<---

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ConsumerServiceImpl>(); //<<---
app.MapGrpcReflectionService();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.Run();
```

`appsettings.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "GrpcHostedOnKestrel": true,
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      },
      "Https": {
        "Url": "https://0.0.0.0:5001"
      }
    },
    "EndpointDefaults": {
      "Protocols": "Http2"
    }
  }
}
```

# How to use it ? (Client)

- Console App
- Add package: InMemoryQueue.Client.Grpc

```csharp
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
        try
        {
            await queueConsumer.PublishAsync(new QueueItemReply()
            {
                Message = "teste"
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


Task<bool> CallBack(QueueItemReply queueItem, CancellationToken arg2)
{
    Console.WriteLine(queueItem);
    return Task.FromResult(true);
}
```

# Running Example:

![Example](https://github.com/fernandozago/InMemoryQueue/blob/main/InMemoryQueue.png?raw=true)
