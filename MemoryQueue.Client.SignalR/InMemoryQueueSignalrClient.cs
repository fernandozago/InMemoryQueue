using MemoryQueue.Transports.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace MemoryQueue.Client.SignalR;


public delegate void QueueInfoReplyEventHandler(QueueInfoReply reply);
public sealed class InMemoryQueueSignalrClient : IAsyncDisposable
{
    const string HubPublishMethodName = "Publish";
    const string HubResetCountersMethodName = "ResetCounters";
    const string HubQueueInfoMethodName = "QueueInfo";
    const string HubConsumeMethodName = "Consume";
    const string HubAckMethodName = "Ack";

    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _token;
    private readonly string _address;
    private readonly string? _queueName;
    private readonly Lazy<Task<HubConnection>> _connection;
    public event QueueInfoReplyEventHandler? OnQueueInfo;

    public InMemoryQueueSignalrClient(string address, string? queueName = null)
    {
        _cts = new CancellationTokenSource();
        _token = _cts.Token;
        _address = address;
        _queueName = queueName;
        _connection = new Lazy<Task<HubConnection>>(async () =>
        {
            var conn = new HubConnectionBuilder()
                .WithUrl(address)
                .WithAutomaticReconnect()
                .Build();
            conn.On<QueueInfoReply>(nameof(QueueInfoReply), v => OnQueueInfo?.Invoke(v));
            await conn.StartAsync(_token).ConfigureAwait(false);
            return conn;
        });
    }

    public async Task PublishAsync(string message)
    {
        if (!_connection.IsValueCreated)
        {
            await _connection.Value.ConfigureAwait(false);
        }
        await _connection.Value.Result.SendAsync(HubPublishMethodName, message, _queueName, _token).ConfigureAwait(false);
    }

    public async Task ResetCountersAsync()
    {
        if (!_connection.IsValueCreated)
        {
            await _connection.Value.ConfigureAwait(false);
        }
        await _connection.Value.Result.SendAsync(HubResetCountersMethodName, _queueName, _token).ConfigureAwait(false);
    }

    public async Task QueueInfoAsync()
    {
        if (!_connection.IsValueCreated)
        {
            await _connection.Value.ConfigureAwait(false);
        }
        await _connection.Value.Result.SendAsync(HubQueueInfoMethodName, _queueName, _token).ConfigureAwait(false);
    }

    public async Task ConsumeAsync(string clientName, Func<string, CancellationToken, Task<bool>> callBack, CancellationToken consumerToken)
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

                await connection.StartAsync(consumerToken).ConfigureAwait(false);
                await foreach (var item in connection.StreamAsync<QueueItemReply>(HubConsumeMethodName, clientName, _queueName, cts.Token))
                {
                    try
                    {
                        await connection.SendAsync(HubAckMethodName, await callBack(item.Message, cts.Token).ConfigureAwait(false), cts.Token).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        await connection.SendAsync(HubAckMethodName, false, cts.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception)
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
            if (_connection.IsValueCreated)
            {
                await _connection.Value.Result.StopAsync().ConfigureAwait(false);
            }
        }
    }
}
