using MemoryQueue.Transports.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;

namespace MemoryQueue.Client.SignalR;


public delegate void QueueInfoReplyEventHandler(QueueInfoReply reply);
public sealed class InMemoryQueueSignalRClient : IAsyncDisposable
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
    private readonly HubConnection _connection;
    public event QueueInfoReplyEventHandler? OnQueueInfo;

    public InMemoryQueueSignalRClient(string address, string? queueName = null)
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
        _connection.Reconnecting += Result_Reconnecting;
    }

    private Task Result_Reconnecting(Exception? arg)
    {
        Debug.WriteLine(arg);
        return Task.CompletedTask;
    }

    private async ValueTask<HubConnection> GetConnection()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync(_token).ConfigureAwait(false);
        }

        return _connection;
    }

    public async Task PublishAsync(string message) =>
        await (await GetConnection().ConfigureAwait(false)).SendAsync(HubPublishMethodName, message, _queueName, _token).ConfigureAwait(false);

    public async Task ResetCountersAsync() =>
        await (await GetConnection().ConfigureAwait(false)).SendAsync(HubResetCountersMethodName, _queueName, _token).ConfigureAwait(false);

    public async Task QueueInfoAsync() =>
        await (await GetConnection().ConfigureAwait(false)).SendAsync(HubQueueInfoMethodName, _queueName, _token).ConfigureAwait(false);

    public async Task ConsumeAsync(string clientName, Func<QueueItemReply, CancellationToken, Task<bool>> callBack, CancellationToken consumerToken)
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
                        await connection.SendAsync(HubAckMethodName, await callBack(item, cts.Token).ConfigureAwait(false), cts.Token).ConfigureAwait(false);
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
            await using var conn = _connection;
            _cts.Cancel();
            try
            {
                await _connection.StopAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {


            }
        }
    }
}
