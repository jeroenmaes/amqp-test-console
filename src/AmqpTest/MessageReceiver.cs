
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace AmqpTest
{
    public class MessageReceiver
    {
        private ConcurrentBag<Task> _tasks;
        private readonly ConnectionSettings _settings;
        private CancellationTokenSource _ct;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private ArtemisReceiver receiver;
        private System.Timers.Timer _timer;

        public MessageReceiver(ConnectionSettings settings, ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _tasks = new ConcurrentBag<Task>();
            _ct = new CancellationTokenSource();
            _loggerFactory = loggerFactory;
            _logger = ApplicationLogging.CreateLogger<MessageReceiver>();
        }

        public async Task Start(Func<Message, Task> messageHandler)
        {            
            receiver = new ArtemisReceiver(_settings, _loggerFactory);
            await receiver.Init(_ct.Token);
            var t = Task.Run(async() => await receiver.GetMessages(messageHandler, _ct.Token), _ct.Token);
            
            var timer = new System.Timers.Timer(3000);            
            timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
            timer.Enabled = true;
            _timer = timer;

            _tasks.Add(t);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _logger.LogInformation($"Statistics::TotalReceivedMessages: {MessageStatistics.TotalReceivedMessages}");
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
            
           receiver.Dispose();
        }
    }
}
