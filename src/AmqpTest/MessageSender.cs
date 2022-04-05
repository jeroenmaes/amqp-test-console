using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace AmqpTest
{
    public class MessageSender
    {
        private ConcurrentBag<Task> _tasks;
        private readonly ConnectionSettings _settings;
        private CancellationTokenSource _ct;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private ArtemisSender sender;
        private System.Timers.Timer _timer;
        private readonly object statisticsLock = new object();


        public MessageSender(ConnectionSettings settings, ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _tasks = new ConcurrentBag<Task>();
            _ct = new CancellationTokenSource();
            _loggerFactory = loggerFactory;
            _logger = ApplicationLogging.CreateLogger<MessageSender>();

        }
        public async Task Start()
        {

            sender = new ArtemisSender(_settings, _loggerFactory);
            await sender.Init(_ct.Token);
            var t = Task.Run(async () => await PutRandomMessages(sender, _ct.Token), _ct.Token);

            var timer = new System.Timers.Timer(3000);
            timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
            timer.Enabled = true;
            _timer = timer;

            _tasks.Add(t);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _logger.LogInformation($"Statistics::TotalSentMessages: {MessageStatistics.TotalSentMessages}");
        }

        private async Task PutRandomMessages(ArtemisSender sender, CancellationToken token)
        {
            var generator = new MessageGenerator();

            for (int i = 1; i <= int.Parse(_settings.SendBatchSize); i++)
            {
                if (!token.IsCancellationRequested)
                {
                    _logger.LogInformation($"BatchMessage {i}/{_settings.SendBatchSize}");
                    await sender.PutMessage(generator.RandomString(1024), Guid.NewGuid().ToString("N").ToUpper(), token);
                    lock (statisticsLock)
                    {
                        MessageStatistics.TotalSentMessages++;
                    }
                    await Task.Delay(100);
                }                                
            }
        }

        public async Task StopAll()
        {
            _timer.Stop();
            _timer.Enabled = false;

            _ct.Cancel();
            await Task.WhenAll(_tasks.ToArray()).ConfigureAwait(false);
            _ct.Dispose();

            _tasks = new ConcurrentBag<Task>();
            _ct = new CancellationTokenSource();

            sender.Dispose();
        }
    }
}
