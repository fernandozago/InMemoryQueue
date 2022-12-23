using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MemoryQueue.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace MemoryQueue.Transports.GRPC.Services
{
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
        private readonly InMemoryQueueManager _queueManager;
        private readonly ILoggerFactory _loggerFactory;

        public ConsumerServiceImpl(InMemoryQueueManager queueManager, IConfiguration configuration, ILoggerFactory loggerFactory)
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
                var inMemoryQueue = _queueManager.GetOrCreateQueue(context.RequestHeaders.GetValue(GRPC_HEADER_QUEUENAME));
                int mainQueueSize = inMemoryQueue.MainChannelCount;
                int retryQueueSize = inMemoryQueue.RetryChannelCount;

                var reply = new QueueInfoReply()
                {
                    QueueName = inMemoryQueue.Name,
                    QueueSize = mainQueueSize + retryQueueSize,
                    MainQueueSize = mainQueueSize,
                    RetryQueueSize = retryQueueSize,

                    ConcurrentConsumers = inMemoryQueue.ConsumersCount,

                    AckCounter = inMemoryQueue.Counters.AckCounter,
                    AckPerSecond = inMemoryQueue.Counters.AckPerSecond,

                    NackCounter = inMemoryQueue.Counters.NackCounter,
                    NackPerSecond = inMemoryQueue.Counters.NackPerSecond,

                    PubCounter = inMemoryQueue.Counters.PubCounter,
                    PubPerSecond = inMemoryQueue.Counters.PubPerSecond,

                    RedeliverCounter = inMemoryQueue.Counters.RedeliverCounter,
                    RedeliverPerSecond = inMemoryQueue.Counters.RedeliverPerSecond,

                    DeliverCounter = inMemoryQueue.Counters.DeliverCounter,
                    DeliverPerSecond = inMemoryQueue.Counters.DeliverPerSecond,

                    AvgAckTimeMilliseconds = inMemoryQueue.Counters.AvgConsumptionMs
                };
                reply.Consumers.AddRange(inMemoryQueue.Consumers.Select(x => ToGrpc(x)));

                return Task.FromResult(reply);
            }
            catch (Exception ex)
            {
                context.ResponseTrailers.Add(GRPC_TRAIL_SERVER_EXCEPTION, ex.Message);
                throw;
            }
        }

        private ConsumerInfoReply ToGrpc(QueueConsumerInfo info)
        {
            var _consumerInfo = new ConsumerInfoReply();
            _consumerInfo.Counters ??= new ConsumerCounters();

            _consumerInfo.Host = info.Host;
            _consumerInfo.Id = info.Id;
            _consumerInfo.Ip = info.Ip;
            _consumerInfo.Name = info.Name;
            _consumerInfo.Type = info.ConsumerType.ToString();

            _consumerInfo.Counters.AckCounter = info.Counters?.AckCounter ?? 0;
            _consumerInfo.Counters.AckPerSecond = info.Counters?.AckPerSecond ?? 0;
            _consumerInfo.Counters.AvgConsumptionMs = info.Counters?.AvgConsumptionMs ?? 0;
            _consumerInfo.Counters.DeliverCounter = info.Counters?.DeliverCounter ?? 0;
            _consumerInfo.Counters.DeliverPerSecond = info.Counters?.DeliverPerSecond ?? 0;
            _consumerInfo.Counters.NackCounter = info.Counters?.NackCounter ?? 0;
            _consumerInfo.Counters.NackPerSecond = info.Counters?.NackPerSecond ?? 0;
            _consumerInfo.Counters.Throttled = info.Counters?.Throttled ?? false;

            return _consumerInfo;
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
                    .Counters.ResetCounters();
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

            InMemoryQueue? memoryQueue = default;
            try
            {
                memoryQueue = (InMemoryQueue)_queueManager.GetOrCreateQueue(context.RequestHeaders.GetValue(GRPC_HEADER_QUEUENAME));
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

            var logger = _loggerFactory.CreateLogger(string.Format(GRPC_QUEUEREADER_LOGGER_CATEGORY, memoryQueue.Name, consumerQueueInfo.ConsumerType, consumerQueueInfo.Name));
            context.CancellationToken.Register(() => logger.LogInformation(LOGMSG_GRPC_REQUEST_CANCELLED));

            var reader = memoryQueue.AddQueueReader(
                    consumerQueueInfo,
                    (item) => WriteAndAckAsync(item, responseStream, requestStream, logger, context.CancellationToken),
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
        private async Task<bool> WriteAndAckAsync(QueueItem item, IServerStreamWriter<QueueItemReply> responseStream, IAsyncStreamReader<QueueItemAck> requestStream, ILogger logger, CancellationToken cancellationToken) =>
            await WriteItemAsync(item, responseStream, logger, cancellationToken).ConfigureAwait(false)
                && await ReadAckAsync(requestStream, logger, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Writes an item into the responseStream
        /// </summary>
        /// <param name="item"></param>
        /// <param name="responseStream"></param>
        /// <param name="logger"></param>
        /// <param name="token"></param>
        /// <returns>true if succeded, otherwise false</returns>
        private async Task<bool> WriteItemAsync(QueueItem item, IServerStreamWriter<QueueItemReply> responseStream, ILogger logger, CancellationToken token)
        {
            try
            {
                if (!_isKestrel)
                {
                    token.ThrowIfCancellationRequested();
                }
                await responseStream.WriteAsync(new QueueItemReply()
                {
                    Message = item.Message,
                    Retrying = item.Retrying,
                    RetryCount = item.RetryCount
                }, _isKestrel ? token : CancellationToken.None).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send the message");
                return false;
            }
        }

        /// <summary>
        /// Awaits for the Ack/Nack from client consumer
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="logger"></param>
        /// <param name="token"></param>
        /// <returns>true = ack, false = nack or fail</returns>
        private static async Task<bool> ReadAckAsync(IAsyncStreamReader<QueueItemAck> requestStream, ILogger logger, CancellationToken token)
        {
            if (await requestStream.MoveNext(token).ConfigureAwait(false))
            {
                return requestStream.Current.Ack;
            }
            //Only call logger.LogWarning when MoveNext(token) returned false -- when Current.Ack is false, should be accepted as valid answer
            else
            {
                logger.LogWarning("Failed to ack the message");
                return false;
            }
        }
    }
}