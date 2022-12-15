# InMemoryQueue
Creates a in memory queue system in .net



# How to use it ?

- Server (Example at Source) -- Using Kestrel:

At Program.cs

Add:
builder.Services.AddSingleton<InMemoryQueueManager>();

Then:
app.MapGrpcService<ConsumerServiceImpl>();

Then at the appsettings
"GrpcHostedOnKestrel": true