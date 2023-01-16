using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MemoryQueue.Base;
using MemoryQueue.Base.Models;
using MemoryQueue.GRPC.Parsers;
using MemoryQueue.Transports.GRPC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoryQueue.GRPC.Transports.GRPC.Services;

public class ConsumerServiceImpl : ConsumerService.ConsumerServiceBase
{
    #region Constants
    private const string GRPC_QUEUEREADER_LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
    private const string GRPC_HOSTED_ON_KESTREL_CONFIG = "GrpcHostedOnKestrel";
    private const string GRPC_HEADER_QUEUENAME = "queuename";
    private const string GRPC_HEADER_CLIENTNAME = "clientname";

    private const string LOGMSG_GRPC_REQUEST_CANCELLED = "Request cancelled and client is being removed";
    private const string LOGMSG_GRPC_STREAM_ENDED = "CONSUMER DISCONNECTED";
    private const string GRPC_TRAIL_SERVER_EXCEPTION = "serverexception";

    #endregion

    private readonly QueueItemAck _ackTrue = new() { Ack = true };
    private readonly Empty _empty = new();
    private readonly bool _isKestrel;
    private readonly IInMemoryQueueManager _queueManager;
    private readonly ILoggerFactory _loggerFactory;

    public ConsumerServiceImpl(IInMemoryQueueManager queueManager, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _isKestrel = bool.TryParse(configuration[GRPC_HOSTED_ON_KESTREL_CONFIG], out bool isKestrel) && isKestrel;
        _queueManager = queueManager;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Add an item to the specified queue
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task<QueueItemAck> Publish(QueueItemRequest request, ServerCallContext context)
    {
        try
        {
            await _queueManager
                .GetOrCreateQueue(context.RequestHeaders.GetValue(GRPC_HEADER_QUEUENAME))
                .EnqueueAsync(request.Message).ConfigureAwait(false);
            return _ackTrue;
        }
        catch (Exception ex)
        {
            context.ResponseTrailers.Add(GRPC_TRAIL_SERVER_EXCEPTION, ex.Message);
            throw;
        }
    }

    public override Task<QueueInfoReply> QueueInfo(Empty request, ServerCallContext context)
    {
        try
        {
            return Task.FromResult(_queueManager.GetOrCreateQueue(context.RequestHeaders.GetValue(GRPC_HEADER_QUEUENAME)).GetInfo().ToReply());
        }
        catch (Exception ex)
        {
            context.ResponseTrailers.Add(GRPC_TRAIL_SERVER_EXCEPTION, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Reset Consumption Counters for the specified queue
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Task<Empty> ResetCounters(Empty request, ServerCallContext context)
    {
        try
        {
            _queueManager
                .GetOrCreateQueue(context.RequestHeaders.GetValue(GRPC_HEADER_QUEUENAME))
                    .ResetCounters();
            return Task.FromResult(_empty);
        }
        catch (Exception ex)
        {
            context.ResponseTrailers.Add(GRPC_TRAIL_SERVER_EXCEPTION, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Starts a reader for the queue and processes it via IAsyncStream and IServerStreamWriter
    /// </summary>
    /// <param name="requestStream"></param>
    /// <param name="responseStream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task BindConsumer(IAsyncStreamReader<QueueItemAck> requestStream, IServerStreamWriter<QueueItemReply> responseStream, ServerCallContext context)
    {

        IInMemoryQueue? memoryQueue = default;
        try
        {
            memoryQueue = _queueManager.GetOrCreateQueue(context.RequestHeaders.GetValue(GRPC_HEADER_QUEUENAME));
        }
        catch (Exception ex)
        {
            context.ResponseTrailers.Add(GRPC_TRAIL_SERVER_EXCEPTION, ex.Message);
            throw;
        }
        string id = Guid.NewGuid().ToString();
        var consumerQueueInfo = new QueueConsumerInfo(QueueConsumerType.GRPC)
        {
            Id = id,
            Host = context.Host,
            Ip = context.Peer,
            Name = context.RequestHeaders.GetValue(GRPC_HEADER_CLIENTNAME) ?? id
        };

        var logger = _loggerFactory.CreateLogger(string.Format(GRPC_QUEUEREADER_LOGGER_CATEGORY, memoryQueue.GetInfo().QueueName, consumerQueueInfo.ConsumerType, consumerQueueInfo.Name));
        using var registration = context.CancellationToken.Register(() =>
        {
            logger.LogInformation(LOGMSG_GRPC_REQUEST_CANCELLED);
        });

        using var reader = memoryQueue.AddQueueReader(
                consumerQueueInfo,
                (item) => WriteAndAckAsync(item, responseStream, requestStream, context.CancellationToken),
                context.CancellationToken);

        await reader.Completed.ConfigureAwait(false);
        memoryQueue.RemoveReader(reader);

        logger.LogTrace(LOGMSG_GRPC_STREAM_ENDED);
    }

    /// <summary>
    /// Group calls (WriteItemToStreamAsync and AwaitAckAsync) to consumer
    /// </summary>
    /// <param name="item"></param>
    /// <param name="responseStream"></param>
    /// <param name="requestStream"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<bool> WriteAndAckAsync(QueueItem item, IServerStreamWriter<QueueItemReply> responseStream, IAsyncStreamReader<QueueItemAck> requestStream, CancellationToken cancellationToken)
    {
        var writeAndAckResults = await Task.WhenAll(
            WriteItemAsync(item, responseStream, cancellationToken),
            ReadAckAsync(requestStream, cancellationToken)
        ).ConfigureAwait(false);
        return writeAndAckResults[0] && writeAndAckResults[1];
    }

    /// <summary>
    /// Writes an item into the responseStream
    /// </summary>
    /// <param name="item"></param>
    /// <param name="responseStream"></param>
    /// <param name="logger"></param>
    /// <param name="token"></param>
    /// <returns>true if succeded, otherwise false</returns>
    private async Task<bool> WriteItemAsync(QueueItem item, IServerStreamWriter<QueueItemReply> responseStream, CancellationToken token)
    {
        await responseStream.WriteAsync(new QueueItemReply()
        {
            Message = item.Message,
            Retrying = item.Retrying,
            RetryCount = item.RetryCount
        }, _isKestrel ? token : CancellationToken.None).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Awaits for the Ack/Nack from client consumer
    /// </summary>
    /// <param name="requestStream"></param>
    /// <param name="logger"></param>
    /// <param name="token"></param>
    /// <returns>true = ack, false = nack or fail</returns>
    private static async Task<bool> ReadAckAsync(IAsyncStreamReader<QueueItemAck> requestStream, CancellationToken token) =>
        await requestStream.MoveNext(token).ConfigureAwait(false) && requestStream.Current.Ack;
}