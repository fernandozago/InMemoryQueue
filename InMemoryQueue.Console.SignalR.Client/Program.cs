// See https://aka.ms/new-console-template for more information
using Microsoft.AspNetCore.SignalR.Client;

Console.WriteLine("Hello, World!");

await Task.Delay(5000);
var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7134/inmemoryqueue/hub")
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("SetVal", c => Console.WriteLine(c));
await connection.StartAsync();

await connection.SendAsync("Publish", "TESTE1", null);
await connection.SendAsync("Publish", "TESTE2", null);
await connection.SendAsync("Publish", "TESTE3", null);
await connection.SendAsync("Publish", "TESTE4", null);
await connection.SendAsync("Publish", "TESTE5", null);

await foreach (var item in connection.StreamAsync<string>("Consume2", null, CancellationToken.None))
{
    //Console.WriteLine(item);
    await connection.SendAsync("Ack");
}

Console.ReadKey();