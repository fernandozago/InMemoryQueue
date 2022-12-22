using InMemoryQueue.Blazor.Host.Grpc;
using InMemoryQueue.Blazor.Host.Grpc.InMemoryConsumers;
using MemoryQueue;
using MemoryQueue.Transports.GRPC.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<GrpcServer>();
builder.Services.AddSingleton<InMemoryQueueManager>();
builder.Services.AddSingleton<ConsumerServiceImpl>();
builder.Services.AddHostedService<InMemoryConsumerBackgroundService>();

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
app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
