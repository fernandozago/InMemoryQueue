using GrpcService2.GrpcImpl;
using GrpcService2.Services;
using MemoryQueue;
using MemoryQueue.Transports.GRPC.Services;
using MemoryQueue.Transports.InMemoryConsumer;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
//builder.Services.AddGrpc();
//builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<GrpcServer>();
builder.Services.AddSingleton<InMemoryQueueManager>();
builder.Services.AddSingleton<ConsumerServiceImpl>();
builder.Services.AddHostedService<InMemoryDefaultQueueConsumerBackground>();

var app = builder.Build();

app.Services.GetRequiredService<GrpcServer>().Start();

// Configure the HTTP request pipeline.
//app.MapGrpcService<GreeterService>();
//app.MapGrpcService<GrpcQueueService>();
//app.MapGrpcReflectionService();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
await app.RunAsync().ConfigureAwait(false);
