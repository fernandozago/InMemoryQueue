using InMemoryQueue.Blazor.Host.Grpc;
using InMemoryQueue.Blazor.Host.Grpc.InMemoryConsumers;
using MemoryQueue.Base.Extensions;
using MemoryQueue.GRPC.Transports.GRPC.Services;
using MemoryQueue.SignalR.Transports.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddInMemoryQueue();
builder.Services.AddSingleton<GrpcServer>();
builder.Services.AddSingleton<ConsumerServiceImpl>();
builder.Services.AddHostedService<InMemoryConsumerBackgroundService>();

builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

app.Services.GetRequiredService<GrpcServer>().Start();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.MapGrpcService<ConsumerServiceImpl>();
//app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapHub<InMemoryQueueHub>("/inmemoryqueue/hub");
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
