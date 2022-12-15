using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MemoryQueue.Client.Grpc.Extensions;
using MemoryQueue.Transports.GRPC;

namespace MemoryQueue.Client.Grpc
{
    public sealed class GrpcQueueConsumer : IAsyncDisposable
    {
        private readonly Channel _channel;
        private readonly ConsumerService.ConsumerServiceClient _client;
        private readonly Metadata _headers;
        private readonly Empty _empty = new ();
        private readonly string? _queueName;

        public GrpcQueueConsumer(string address, string? queueName = default)
        {
            _queueName = queueName;
            var opts = new List<ChannelOption>
            {
                new ChannelOption("grpc.keepalive_time_ms", (int)TimeSpan.FromMinutes(1).TotalMilliseconds),
                new ChannelOption("grpc.keepalive_timeout_ms", (int)TimeSpan.FromSeconds(10).TotalMilliseconds)
            };
            //opts.Add(new ChannelOption("grpc.keepalive_permit_without_calls", 1));
            //opts.Add(new ChannelOption("grpc.http2.min_time_between_pings_ms", connOptions.TimeBetweenPings));
            //opts.Add(new ChannelOption("grpc.http2.max_pings_without_data", 0));

            _channel = new Channel(address, ChannelCredentials.Insecure, opts);
            _client = new ConsumerService.ConsumerServiceClient(_channel);
            _headers = new Metadata();
            if (queueName is not null)
            {
                _headers.Add("queuename", queueName);
            };
        }

        public AsyncUnaryCall<QueueItemAck> PublishAsync(QueueItemReply item)
        {
            return _client.PublishAsync(item, headers: _headers, deadline: DateTime.UtcNow.AddSeconds(1));
        }
            

        public AsyncUnaryCall<QueueInfoReply> QueueInfoAsync() =>
            _client.QueueInfoAsync(_empty, headers: _headers, deadline: DateTime.UtcNow.AddSeconds(1));

        public async Task Consume(string name, Func<QueueItemReply, CancellationToken, Task<bool>> callBack, CancellationToken externalToken)
        {
            while (!externalToken.IsCancellationRequested)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, _channel.ShutdownToken);
                var token = cts.Token;
                try
                {
                    var metadata = new Metadata
                    {
                        { "clientname", name },
                    };
                    if (_queueName is not null)
                    {
                        metadata.Add("queuename", _queueName);
                    }

                    using var stream = _client.BindConsumer(headers: metadata, cancellationToken: token);
                    await foreach (var item in stream.ResponseStream.ReadAllAsync(token).ConfigureAwait(false))
                    {
                        bool ack = false;
                        try
                        {
                            ack = await callBack(item, token).ConfigureAwait(false);
                        }
                        catch
                        {
                            ack = false;
                        }
                        finally
                        {
                            await stream.RequestStream.WriteAsync(new QueueItemAck()
                            {
                                Ack = ack
                            }, CancellationToken.None).ConfigureAwait(false);
                            token.ThrowIfCancellationRequested();
                        }
                    }
                }
                catch(RpcException ex)
                {
                    if (ex.Trailers.GetValue("serverexception") is string serverException)
                    {
                        Console.WriteLine($"Server Exception: {serverException}");
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"Server Exception: {ex}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(20), token);
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _channel.ShutdownAsync().ConfigureAwait(false);
        }

        public AsyncUnaryCall<Empty> ResetCountersAsync() =>
            _client.ResetCountersAsync(new Empty());
    }
}