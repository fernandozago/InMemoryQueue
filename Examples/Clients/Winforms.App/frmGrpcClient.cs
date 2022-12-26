using GrpcClient4.QueueInfoPropertyGrid;
using MemoryQueue.Client.Grpc;
using MemoryQueue.Client.SignalR;
using MemoryQueue.Transports.GRPC;
using RandomNameGeneratorLibrary;
using System.Diagnostics;
using System.Text.Json;

namespace GrpcClient2
{
    public partial class frmGrpcClient : Form
    {
        private readonly IProgress<QueueInfoReply> _progress;
        private readonly GrpcQueueConsumer _consumer;
        private readonly Dictionary<Task, CancellationTokenSource> _consumers = new();
        private readonly PersonNameGenerator _personNameGenerator = new();
        private readonly string _queueName;

        double chartYMax = 0;
        const int time = 30;
        double[] deliverPerSecond = new double[time];
        double[] ackPerSecond = new double[time];
        double[] pubPerSecond = new double[time];
        double[] nackPerSecond = new double[time];
        double[] redeliverPerSecond = new double[time];
        double[] chartMaxYAxis = new double[time];
        public frmGrpcClient(string host, string queueName)
        {
            _queueName = queueName;
            _consumer = new(host, queueName: queueName);
            _progress = new Progress<QueueInfoReply>(UpdateForm);

            _ = StartTimer();
            InitializeComponent();
            Text = $"[{_queueName}] - {host}";

            formsPlot1.Plot.Grid(false);
            formsPlot1.Plot.Legend(true, ScottPlot.Alignment.UpperLeft);
            formsPlot1.Plot.Title("Queue Data p/ Second");
            formsPlot1.Plot.YAxis.Grid(true);

            var plotLine = formsPlot1.Plot.AddSignal(pubPerSecond);
            plotLine.Label = "Incoming";
            plotLine.LineWidth = 2;
            plotLine.Color = Color.Gray;

            plotLine = formsPlot1.Plot.AddSignal(deliverPerSecond);
            plotLine.Label = "Deliver";
            plotLine.LineWidth = 5;
            plotLine.Color = Color.Green;

            plotLine = formsPlot1.Plot.AddSignal(redeliverPerSecond);
            plotLine.Label = "Redeliver";
            plotLine.LineWidth = 5;
            plotLine.Color = Color.DarkGreen;

            plotLine = formsPlot1.Plot.AddSignal(ackPerSecond);
            plotLine.Label = "Ack";
            plotLine.LineWidth = 1;
            plotLine.Color = Color.Gold;

            plotLine = formsPlot1.Plot.AddSignal(nackPerSecond);
            plotLine.Label = "Nack";
            plotLine.LineWidth = 1;
            plotLine.Color = Color.Red;

            var max = chartMaxYAxis.Max();
            formsPlot1.Plot.SetAxisLimits(yMin: 0, yMax: Math.Max(max + max * 0.05, 5), xMin: 0, xMax: time);
            formsPlot1.Refresh();
        }

        private async Task RandomPub()
        {
            while (_playPause)
            {
                await Task.Delay(TimeSpan.FromSeconds(.5));
                await Parallel.ForEachAsync(Enumerable.Range(0, Random.Shared.Next(1, 30_000)), async (a, t) =>
                {
                    try
                    {
                        await _consumer.PublishAsync(new QueueItemRequest()
                        {
                            Message = JsonSerializer.Serialize(new Data(Guid.Empty, DateTime.Now, "A"))
                        });
                    }
                    catch
                    {
                        //
                    }
                });
            }
        }

        private void AppendToArray(ref double[] arr, double value)
        {
            Array.Copy(arr, 1, arr, 0, arr.Length - 1);
            arr[^1] = value;
            chartYMax = Math.Max(chartYMax, value);
        }

        private void UpdateForm(QueueInfoReply obj)
        {
            chartYMax = 0;
            AppendToArray(ref deliverPerSecond, obj.DeliverPerSecond);
            AppendToArray(ref redeliverPerSecond, obj.RedeliverPerSecond);
            AppendToArray(ref ackPerSecond, obj.AckPerSecond);
            AppendToArray(ref nackPerSecond, obj.NackPerSecond);
            AppendToArray(ref pubPerSecond, obj.PubPerSecond);
            AppendToArray(ref chartMaxYAxis, chartYMax);

            var max = chartMaxYAxis.Max();
            formsPlot1.Plot.SetAxisLimits(yMin: 0, yMax: Math.Max(max + max * 0.05, 5), xMin: 0, xMax: time);
            formsPlot1.Refresh();

            if (!propertyGrid1.IsDisposed)
            {
                propertyGrid1.SelectedObject ??= new QueueInfoReplyPropertyGrid();
                var objRef = (QueueInfoReplyPropertyGrid)propertyGrid1.SelectedObject;
                obj.Convert(ref objRef);
                propertyGrid1.Refresh();
            }
        }

        private bool _playPause;

        private async Task StartTimer()
        {
            await Task.Yield();
            var perTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await perTimer.WaitForNextTickAsync().ConfigureAwait(false) && !Disposing)
            {
                await GetQueueInfoAsync().ConfigureAwait(false);
            }
        }

        private async Task GetQueueInfoAsync()
        {
            try
            {
                _progress.Report(await _consumer.QueueInfoAsync());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            await GetQueueInfoAsync();
            button1.Enabled = true;
        }

        const int qtd = 1_000_000;
        //const int qtd = 100_000;

        private void btnAddConsumer_Click(object sender, EventArgs e)
        {
            try
            {
                var cts = new CancellationTokenSource();
                var consumerTask = _consumer.Consume(_personNameGenerator.GenerateRandomFirstAndLastName(), static (item, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return Task.FromResult(true);
                }, cts.Token);
                _consumers.Add(consumerTask, cts);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private async void btnRemoveConsumer_Click(object sender, EventArgs e)
        {
            btnRemoveConsumer.Enabled = false;
            try
            {
                if (_consumers.LastOrDefault() is KeyValuePair<Task, CancellationTokenSource> val && val.Key is not null)
                {
                    using (_consumers[val.Key])
                    {
                        _consumers[val.Key].Cancel();
                        await val.Key;
                        _consumers.Remove(val.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                btnRemoveConsumer.Enabled = true;
            }
        }

        private async void btnResetCounters_Click(object sender, EventArgs e)
        {
            await _consumer.ResetCountersAsync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _playPause = !_playPause;
            _ = RandomPub();
            if (_playPause)
            {
                button2.Text = "Stop";
            }
            else
            {
                button2.Text = "Start";
            }
        }

        private async void btnPub_Click(object sender, EventArgs e)
        {
            btnPub.Enabled = false;
            await Parallel.ForEachAsync(Enumerable.Range(0, qtd), async (a, t) =>
            {
                try
                {
                    await _consumer.PublishAsync(new QueueItemRequest()
                    {
                        Message = JsonSerializer.Serialize(new Data(Guid.NewGuid(), DateTime.Now, "A"))
                    });
                }
                catch (Exception)
                {
                    //
                }
            });
            btnPub.Enabled = true;
        }

        private async void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _playPause = false;
            await _consumer.DisposeAsync();
        }

        public record Data(Guid Info, DateTime CreateDate, string Value);
    }
}