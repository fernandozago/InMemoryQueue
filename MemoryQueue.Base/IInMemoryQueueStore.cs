using Azure.Identity;
using Dapper;
using MemoryQueue.Base.Models;
using MemoryQueue.Base.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using Z.Dapper.Plus;

namespace MemoryQueue.Base
{
    public class InMemoryQueueStore
    {
        private readonly BatchBlock<QueueItemDbChange> _batchBlock;
        private readonly TransformBlock<QueueItemDbChange[], (QueueItemDbChange[], QueueItemDbChange[], double)> _transformBlock;
        private readonly ActionBlock<(QueueItemDbChange[], QueueItemDbChange[], double)> _actionBlockUpsert;
        private readonly Timer _t;
        //private readonly StreamWriter _logFile;
        private readonly ILogger<InMemoryQueueStore> _logger;
        private readonly string _queueName;
        private readonly SqlConnection _connection;
        private readonly DapperPlusContext _dapperPlusContext;
        private int _batchBlockInputCount = 0;
        public int _balance = 0;
        private long _lastTrigger;

        public InMemoryQueueStore(string queueName, ILoggerFactory loggerFactory)
        {
            //_logFile = new StreamWriter(new FileStream("E:\\Output.log", FileMode.Truncate));
            //_logFile.AutoFlush = true;
            _logger = loggerFactory.CreateLogger<InMemoryQueueStore>();
            _queueName = queueName;

            _batchBlock = new BatchBlock<QueueItemDbChange>(15_000, new GroupingDataflowBlockOptions());
            _transformBlock = new TransformBlock<QueueItemDbChange[], (QueueItemDbChange[], QueueItemDbChange[], double)>(TransformBatch, new ExecutionDataflowBlockOptions()
            {
                EnsureOrdered = true,
                MaxDegreeOfParallelism = 4
            });
            _actionBlockUpsert = new(ExecuteUpsertDelete);

            _t = new Timer(new TimerCallback(_ =>
            {
                if (_batchBlockInputCount > 0 && StopwatchEx.GetElapsedTime(_lastTrigger).TotalSeconds > 5)
                {
                    _logger.LogInformation($"Triggered from Timer {DateTime.Now}");
                    _lastTrigger = Stopwatch.GetTimestamp();
                    _batchBlock.TriggerBatch();
                }
            }), null, 1000, 1000);

            _batchBlock.LinkTo(_transformBlock);
            _transformBlock.LinkTo(_actionBlockUpsert, x => x.Item1.Length > 0 || x.Item2.Length > 0);

            if (!File.Exists(@$"E:\{_queueName}_log.ldf"))
            {
                File.Copy(@"E:\Template_log.ldf", @$"E:\{_queueName}_log.ldf");
            }


            if (!File.Exists(@$"E:\{_queueName}.mdf"))
            {
                File.Copy(@"E:\Template.mdf", @$"E:\{_queueName}.mdf");
            }

            _connection = new SqlConnection($"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=E:\\{_queueName}.mdf;Integrated Security=True;Connect Timeout=30");
            _connection.Open();

            _dapperPlusContext = new DapperPlusContext(_connection);
            _dapperPlusContext.Entity<QueueItem>().Table("QueueItems").Identity(x => x.Id).KeepIdentity(true).AutoMap();
            TruncateAsync().GetAwaiter().GetResult();
        }

        private (QueueItemDbChange[], QueueItemDbChange[], double) TransformBatch(QueueItemDbChange[] batch)
        {
            var lt = Stopwatch.GetTimestamp();
            _lastTrigger = lt;

            Interlocked.Add(ref _batchBlockInputCount, batch.Length * -1);

            var groupAndSplit = batch
                .GroupBy(x => x.Id, x => x)
                .AsParallel()
                .Where(FilterItems)
                .Select(SelectItem)
                .GroupBy(x => x.Upsert)
                .ToDictionary(x => x.Key, x => x.ToArray());

            var toUpsert = groupAndSplit.GetValueOrDefault(true, Array.Empty<QueueItemDbChange>());
            var toDelete = groupAndSplit.GetValueOrDefault(false, Array.Empty<QueueItemDbChange>());
            //_logger.LogInformation($"Processed Batch {batch.Length} -- {toUpsert.Count}|{toDelete.Count} -- {StopwatchEx.GetElapsedTime(lt).TotalMilliseconds:N15} ms");
            return (toUpsert, toDelete, StopwatchEx.GetElapsedTime(lt).TotalMilliseconds);
        }

        private static bool FilterItems(IGrouping<Guid, QueueItemDbChange> items)
        {
            if (items.Count() == 1)
            {
                return true;
            }

            bool haveInsert = items.Any(x => x.ChangeType == QueueItemDbChangeType.Insert);
            bool haveDelete = items.Any(x => x.ChangeType == QueueItemDbChangeType.Delete);
            if (haveInsert && haveDelete)
            {
                return false;
            }

            return true;
        }

        private static QueueItemDbChange SelectItem(IGrouping<Guid, QueueItemDbChange> groupedCommands)
        {
            var lastCommand = groupedCommands.Last();
            if (groupedCommands.Any(x => x.ChangeType == QueueItemDbChangeType.Insert))
            {
                lastCommand.Balance++;
            }

            if (lastCommand.ChangeType == QueueItemDbChangeType.Delete)
            {
                lastCommand.Balance--;
            }
            return lastCommand;
        }

        //int packageId = 0;
        private async Task ExecuteUpsertDelete((QueueItemDbChange[] upsert, QueueItemDbChange[] delete, double processBatch) triple)
        {
            //packageId++;
            //_logFile.WriteLine($"Starting Save Package {packageId}");
            _lastTrigger = Stopwatch.GetTimestamp();
            var saveTs = Stopwatch.GetTimestamp();
            var changes = 0;
            if (triple.upsert.Length > 0)
            {
                await Task.Factory.StartNew(() =>
                {
                    try
                    {
                        _dapperPlusContext.BulkMerge(triple.upsert.Select(x => x.Item));
                        changes += triple.upsert.Sum(x => x.Balance);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error on BulkMerge");
                    }

                });
            }

            if (triple.delete.Length > 0)
            {
                await Task.Factory.StartNew(() =>
                {

                    try
                    {
                        _dapperPlusContext.BulkDelete(triple.delete.Select(x => x.Item));
                        changes += triple.delete.Sum(x => x.Balance);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error on BulkDelete");
                    }
                });
            }
            //_logFile.WriteLine($"Save Package Done {packageId}");
            Interlocked.Add(ref _balance, changes);
            _logger.LogInformation($"Pending Batches To Save: {_actionBlockUpsert.InputCount} -> " +
                $"Db Changes Diff: {changes} - StoredItems: {_balance} -- ProcessBatch Took: {triple.processBatch:N5}ms -- SaveToDb Took: {StopwatchEx.GetElapsedTime(saveTs).TotalMilliseconds:N5}ms");
        }

        public async Task TruncateAsync()
        {
            _logger.LogInformation("Truncating Table");
            await _connection.ExecuteAsync("TRUNCATE TABLE QueueItems");
        }

        public async Task<QueueItem> UpsertAsync(QueueItem item)
        {
            var command = new QueueItemDbChange(item, true);
            await _batchBlock.SendAsync(command).ConfigureAwait(false);
            Interlocked.Increment(ref _batchBlockInputCount);
            return item;
        }

        public async Task DeleteAsync(QueueItem item)
        {
            var command = new QueueItemDbChange(item, false);
            await _batchBlock.SendAsync(command).ConfigureAwait(false);
            Interlocked.Increment(ref _batchBlockInputCount);
        }

        public void TriggerBatch()
        {
            _batchBlock.TriggerBatch();
        }
    }
}

