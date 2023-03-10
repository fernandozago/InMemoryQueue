using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MemoryQueue.Client.Grpc.Extensions;
using MemoryQueue.Transports.GRPC;
using Microsoft.Extensions.Logging;

namespace MemoryQueue.Client.Grpc
{
    public sealed class GrpcQueueConsumer : IAsyncDisposable
    {
        private const string LOGGERFACTORY_CATEGORY = $"{nameof(GrpcQueueConsumer)}.{{0}}";
        private const string GRPC_CALL_HEADER_QUEUENAME = "queuename";
        private const string GRPC_CALL_HEADER_CLIENTNAME = "clientname";
        private readonly Channel _channel;
        private readonly ConsumerService.ConsumerServiceClient _client;
        private readonly Metadata _headers;
        private readonly Empty _empty = new ();
        private readonly ILogger? _logger;
        private readonly string? _queueName;

        public GrpcQueueConsumer(string address, ILoggerFactory? loggerFactory = default, string? queueName = default)
        {
            _logger = loggerFactory?.CreateLogger(string.Format(LOGGERFACTORY_CATEGORY, string.IsNullOrWhiteSpace(queueName) ? "Default" : queueName));
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
                _headers.Add(GRPC_CALL_HEADER_QUEUENAME, queueName);
            };
        }

        public AsyncUnaryCall<QueueItemAck> PublishAsync(QueueItemRequest item) =>
            _client.PublishAsync(item, headers: _headers, deadline: DateTime.UtcNow.AddSeconds(1));

        public AsyncUnaryCall<QueueItemAck> PublishAsync(string item) =>
            PublishAsync(new QueueItemRequest() { Message = item });

        public Task<QueueItemAck[]> PublishAllAsync(IEnumerable<string> items) =>
            Task.WhenAll(items.Select(i => PublishAsync(i).ResponseAsync));   

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
                        { GRPC_CALL_HEADER_CLIENTNAME, name },
                    };
                    if (_queueName is not null)
                    {
                        metadata.Add(GRPC_CALL_HEADER_QUEUENAME, _queueName);
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

                            token.ThrowIfCancellationRequested();
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
                        _logger?.LogError(ex, serverException);
                        break;
                    }
                    else
                    {
                        _logger?.LogError(ex, "Client got an RPC error");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Client got an generic error");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(20), token).ConfigureAwait(false);
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