# InMemoryQueue
Creates a in memory queue system in .net



# How to use it ?

- Server using `Kestrel`:

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
