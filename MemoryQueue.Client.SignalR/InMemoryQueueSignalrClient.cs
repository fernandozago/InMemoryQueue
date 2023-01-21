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

    private readonly CancellationTokenSource _instanceCancellationTokenSource;
    private readonly CancellationToken _instanceToken;
    private readonly string _address;
    private readonly string? _queueName;
    private readonly HubConnection _connection;
    public event QueueInfoReplyEventHandler? OnQueueInfo;

    public InMemoryQueueSignalRClient(string address, string? queueName = null)
    {
        _instanceCancellationTokenSource = new CancellationTokenSource();
        _instanceToken = _instanceCancellationTokenSource.Token;
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
            await _connection.StartAsync(_instanceToken).ConfigureAwait(false);
        }

        return _connection;
    }

    public async Task PublishAsync(string message) =>
        await (await GetConnection().ConfigureAwait(false)).SendAsync(HubPublishMethodName, message, _queueName, _instanceToken).ConfigureAwait(false);

    public async Task ResetCountersAsync() =>
        await (await GetConnection().ConfigureAwait(false)).SendAsync(HubResetCountersMethodName, _queueName, _instanceToken).ConfigureAwait(false);

    public async Task QueueInfoAsync() =>
        await (await GetConnection().ConfigureAwait(false)).SendAsync(HubQueueInfoMethodName, _queueName, _instanceToken).ConfigureAwait(false);

    public async Task ConsumeAsync(string clientName, Func<QueueItemReply, CancellationToken, Task<bool>> callBack, CancellationToken externalToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, _instanceToken);
        var consumeToken = cts.Token;
        while (!consumeToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = new HubConnectionBuilder()
                    .WithUrl(_address)
                    .Build();

                connection.Closed += _ =>
                {
                    cts.Cancel();
                    return Task.CompletedTask;
                };

                await connection.StartAsync(consumeToken).ConfigureAwait(false);
                connection.On<QueueItemReply, bool>("ProcessQueueItem", async (s) =>
                {
                    try
                    {
                        return await callBack(s, consumeToken).ConfigureAwait(false);
                    }
                    catch(Exception ex)
                    {
                        return false;
                    }
                });
                await connection.SendAsync("ConsumeNew", _queueName, _instanceToken).ConfigureAwait(false);

                try
                {
                    await Task.Delay(Timeout.Infinite, consumeToken);
                }
                catch (Exception)
                {
                    //
                }
                
                await connection.StopAsync().ConfigureAwait(false);
                connection.Remove("ProcessQueueItem");

                //await foreach (var item in connection.StreamAsync<QueueItemReply>(HubConsumeMethodName, clientName, _queueName, cts.Token))
                //{
                //    try
                //    {
                //        await connection.SendAsync(HubAckMethodName, await callBack(item, cts.Token).ConfigureAwait(false), cts.Token).ConfigureAwait(false);
                //    }
                //    catch (Exception)
                //    {
                //        await connection.SendAsync(HubAckMethodName, false, cts.Token).ConfigureAwait(false);
                //    }
                //}
            }
            catch (Exception ex)
            {
                if (!consumeToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), consumeToken);
                    }
                    catch (Exception)
                    {
                        //
                    }
                }
                else
                {
                    
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        using (_instanceCancellationTokenSource)
        {
            await using var conn = _connection;
            _instanceCancellationTokenSource.Cancel();
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
