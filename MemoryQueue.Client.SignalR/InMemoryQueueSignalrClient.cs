using MemoryQueue.Transports.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace MemoryQueue.Client.SignalR;


public delegate void QueueInfoReplyEventHandler(QueueInfoReply reply);
public sealed class InMemoryQueueSignalrClient : IAsyncDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _token;
    private readonly string _address;
    private readonly string? _queueName;
    private readonly HubConnection _connection;
    public event QueueInfoReplyEventHandler? OnQueueInfo;

    public InMemoryQueueSignalrClient(string address, string? queueName = null)
    {
        _cts = new CancellationTokenSource();
        _token = _cts.Token;
        _address = address;
        _queueName = queueName;
        _connection = new HubConnectionBuilder()
            .WithUrl(address)
            .WithAutomaticReconnect()
            .Build();


        _connection.On<QueueInfoReply>(nameof(QueueInfoReply), v => OnQueueInfo?.Invoke(v));
        _connection.StartAsync().GetAwaiter().GetResult();
    }

    public Task PublishAsync(string message) =>
        _connection.SendAsync("Publish", message, _queueName);

    public Task ResetCountersAsync() =>
        _connection.SendAsync("ResetCounters", _queueName);
    public Task QueueInfoAsync() =>
        _connection.SendAsync("QueueInfo", _queueName);

    public async Task Consume(string clientName, Func<string, CancellationToken, Task<bool>> callBack, CancellationToken consumerToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(consumerToken, _token);
        while (!cts.IsCancellationRequested)
        {
            try
            {
                await using var connection = new HubConnectionBuilder()
                    .WithUrl(_address)
                    .WithAutomaticReconnect()
                    .Build();

                await connection.StartAsync(consumerToken);
                await foreach (var item in connection.StreamAsync<QueueItemReply>("Consume", clientName, _queueName, cts.Token))
                {
                    try
                    {
                        await connection.SendAsync("Ack", await callBack(item.Message, cts.Token), cts.Token);
                    }
                    catch (Exception)
                    {
                        await connection.SendAsync("Ack", false, cts.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
                    }
                    catch (Exception)
                    {
                        //
                    }
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        using (_cts)
        {
            _cts.Cancel();
            await _connection.StopAsync();
        }
    }
}
