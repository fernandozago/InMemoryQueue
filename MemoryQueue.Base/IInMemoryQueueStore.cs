using MemoryQueue.Base.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MemoryQueue.Base
{
    public class InMemoryQueueStore
    {
        private readonly BatchBlock<SqlCommand> _batchBlock;
        private readonly ActionBlock<SqlCommand[]> _actionBlock;
        private readonly ILogger<InMemoryQueueStore> _logger;
        private readonly string _queueName;
        private readonly SqlConnection _connection;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private const string UPSERT = @"UPDATE QueueItems SET Retrying=@Retrying, RetryCount=@RetryCount,Message=@Message where Id = @Id
if @@ROWCOUNT = 0
INSERT INTO QueueItems(Id, Retrying, RetryCount, Message) VALUES(@Id, @Retrying, @RetryCount, @Message)";
        private const string DELETE = "DELETE FROM QueueItems WHERE Id = @Id";

        public InMemoryQueueStore(string queueName, ILoggerFactory loggerFactory)
        {
            _batchBlock = new BatchBlock<SqlCommand>(10_000);
            _actionBlock = new ActionBlock<SqlCommand[]>(ExecuteCommand, new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = 1
            });
            _batchBlock.LinkTo(_actionBlock);
            _logger = loggerFactory.CreateLogger<InMemoryQueueStore>();
            _queueName = queueName;

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
            TruncateAsync().GetAwaiter().GetResult();
        }

        public async Task TruncateAsync()
        {
            SqlCommand command = _connection.CreateCommand();
            command.CommandText = "TRUNCATE TABLE QueueItems";
            await _batchBlock.SendAsync(command);
            //await ExecuteCommand(command).ConfigureAwait(false);
        }

        public async Task<QueueItem> UpsertAsync(QueueItem item)
        {
            var command = _connection.CreateCommand();
            command.CommandText = UPSERT;

            command.Parameters.Add("Id", System.Data.SqlDbType.UniqueIdentifier);
            command.Parameters[0].Value = item.Id;

            command.Parameters.Add("Retrying", System.Data.SqlDbType.Bit);
            command.Parameters[1].Value = item.Retrying ? 1 : 0;

            command.Parameters.Add("RetryCount", System.Data.SqlDbType.Int);
            command.Parameters[2].Value = item.RetryCount;

            command.Parameters.Add("Message", System.Data.SqlDbType.Text);
            command.Parameters[3].Value = item.Message;
            await _batchBlock.SendAsync(command).ConfigureAwait(false);
            return item;
        }

        public async Task DeleteAsync(Guid id)
        {
            var command = _connection.CreateCommand();
            command.CommandText = DELETE;
            command.Parameters.Add("Id", System.Data.SqlDbType.UniqueIdentifier);
            command.Parameters[0].Value = id;
            await _batchBlock.SendAsync(command).ConfigureAwait(false);
        }

        private async Task ExecuteCommand(SqlCommand[] command)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            using var trans = (SqlTransaction)await _connection.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                _logger.LogInformation("Evaluating...");
                
                foreach (var cmd in command)
                {
                    using (cmd)
                    {
                        cmd.Transaction = trans;
                        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                _logger.LogInformation("Commiting...");
                await trans.CommitAsync().ConfigureAwait(false);
                _semaphore.Release();
                _logger.LogInformation($"Wrote {command.Count()} commands -- pending: {_batchBlock.OutputCount + _actionBlock.InputCount}");
            }
        }
    }
}
