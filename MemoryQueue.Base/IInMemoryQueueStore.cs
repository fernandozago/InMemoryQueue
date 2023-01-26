using Azure.Identity;
using Dapper;
using MemoryQueue.Base.Models;
using MemoryQueue.Base.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using System.Xml.Schema;
using Z.Dapper.Plus;

namespace MemoryQueue.Base
{
    public class InMemoryQueueStore
    {
        private readonly BatchBlock<MyCommand> _batchBlock;
        private readonly TransformBlock<MyCommand[], (List<MyCommandKey>, List<MyCommandKey>, double)> _transformBlock;
        private readonly ActionBlock<(List<MyCommandKey>, List<MyCommandKey>, double)> _actionBlockUpsert;
        private readonly CancellationTokenSource _cts;
        private readonly Timer _t;
        private readonly StreamWriter _logFile;
        private readonly ILogger<InMemoryQueueStore> _logger;
        private readonly string _queueName;
        private readonly SqlConnection _connection;
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly DapperPlusContext _dapperPlusContext;
        private int _batchBlockInputCount = 0;
        public int _balance = 0;
        private long _lastTrigger;

        public InMemoryQueueStore(string queueName, ILoggerFactory loggerFactory)
        {
            _logFile = new StreamWriter(new FileStream("E:\\Output.log", FileMode.Truncate));
            _logFile.AutoFlush = true;
            _logger = loggerFactory.CreateLogger<InMemoryQueueStore>();
            _queueName = queueName;

            _batchBlock = new BatchBlock<MyCommand>(5_000, new GroupingDataflowBlockOptions());
            _transformBlock = new TransformBlock<MyCommand[], (List<MyCommandKey>, List<MyCommandKey>, double)>(TransformBatch, new ExecutionDataflowBlockOptions()
            {
                EnsureOrdered = true,
                MaxDegreeOfParallelism = 8
            });
            _actionBlockUpsert = new (ExecuteUpsertDelete, new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = 1
            });

            _t = new Timer(new TimerCallback(_ =>
            {
                //_logger.LogInformation($"Pending Batches: {_transformBlock.OutputCount}");
                if (_batchBlockInputCount > 0 && StopwatchEx.GetElapsedTime(_lastTrigger).TotalSeconds > 5)
                {
                    _logger.LogInformation($"Triggered from Timer {DateTime.Now}");
                    _lastTrigger = Stopwatch.GetTimestamp();
                    _batchBlock.TriggerBatch();
                }
            }), null, 1000, 1000);

            _batchBlock.LinkTo(_transformBlock);
            _transformBlock.LinkTo(_actionBlockUpsert);//, x => x.Item1.Count > 0 || x.Item2.Count > 0);

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

        private (List<MyCommandKey>, List<MyCommandKey>, double) TransformBatch(MyCommand[] batch)
        {
            var lt = Stopwatch.GetTimestamp();
            _lastTrigger = lt;

            Interlocked.Add(ref _batchBlockInputCount, batch.Count() * -1);

            var groupAndSplit = batch
                .AsParallel()
                .GroupBy(x => new MyCommandKey(x.Id), x => x)
                .Where(FilterItems)
                .Select(x => x.Key)
                .GroupBy(x => x.Command.Upsert)
                .ToDictionary(x => x.Key, x => x.Select(x => x).ToList());

            var toUpsert = groupAndSplit.GetValueOrDefault(true, new List<MyCommandKey>());
            var toDelete = groupAndSplit.GetValueOrDefault(false, new List<MyCommandKey>());
            //_logger.LogInformation($"Processed Batch {batch.Length} -- {toUpsert.Count}|{toDelete.Count} -- {StopwatchEx.GetElapsedTime(lt).TotalMilliseconds:N15} ms");
            return (toUpsert, toDelete, StopwatchEx.GetElapsedTime(lt).TotalMilliseconds);
        }

        private bool FilterItems(IGrouping<MyCommandKey, MyCommand> items)
        {
            int insertCount = items.Count(x => x.Upsert && !x.Item.Retrying); //Items to be inserted

            if (items.Count() == 1 || (items.Count(x => !x.Upsert) is int deleteCount && deleteCount == 0))
            {
                if (items.Last().Upsert && insertCount != 0)
                {
                    items.Key.Balance = 1;
                }
                else
                {
                    items.Key.Balance = -1;
                }

                items.Key.Command = items.Last();
                return true;
            }

            if (insertCount == 1 && deleteCount == 1)
            {
                return false;
            }

            items.Key.Balance = -1;
            items.Key.Command = items.Last();
            return true;
        }

        private async Task ExecuteUpsertDelete((List<MyCommandKey> upsert, List<MyCommandKey> delete, double processBatch) triple)
        {
            _lastTrigger = Stopwatch.GetTimestamp();
            var saveTs = Stopwatch.GetTimestamp();
            var changes = 0;
            if (triple.upsert.Count > 0)
            {
                await Task.Factory.StartNew(() =>
                {
                    try
                    {
                        _dapperPlusContext.BulkMerge(triple.upsert.Select(x => x.Command.Item));

                        var insertChanges = triple.upsert.Sum(x => x.Balance);
                        changes += insertChanges;
                        Interlocked.Add(ref _balance, insertChanges);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error on BulkMerge");
                    }

                });
            }

            if (triple.delete.Count > 0)
            {
                await Task.Factory.StartNew(() =>
                {

                    try
                    {
                        _dapperPlusContext.BulkDelete(triple.delete.Select(x => x.Command.Item));

                        var deleteChanges = triple.delete.Sum(x => x.Balance);
                        changes += Math.Abs(deleteChanges);
                        Interlocked.Add(ref _balance, deleteChanges);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error on BulkDelete");
                    }
                });
            }
            _logger.LogInformation($"Completed Batches: {_transformBlock.InputCount}|{_transformBlock.OutputCount} -> " +
                $"Db Batch Changes: {changes} - StoredItems: {_balance} -- ProcessBatch Took: {triple.processBatch:N5}ms -- SaveToDb Took: {StopwatchEx.GetElapsedTime(saveTs).TotalMilliseconds:N5}ms");
        }

        public async Task TruncateAsync()
        {
            _logger.LogInformation("Truncating Table");
            await _connection.ExecuteAsync("TRUNCATE TABLE QueueItems");
        }

        public async Task<QueueItem> UpsertAsync(QueueItem item)
        {
            await _batchBlock.SendAsync(new MyCommand(item, true)).ConfigureAwait(false);
            Interlocked.Increment(ref _batchBlockInputCount);
            return item;
        }

        public async Task DeleteAsync(QueueItem item)
        {
            await _batchBlock.SendAsync(new MyCommand(item, false)).ConfigureAwait(false);
            Interlocked.Increment(ref _batchBlockInputCount);
        }

        public void TriggerBatch()
        {
            _batchBlock.TriggerBatch();
        }
    }
}

