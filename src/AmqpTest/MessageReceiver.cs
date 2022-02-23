
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AmqpTest
{
    public class MessageReceiver
    {
        private ConcurrentBag<Task> _tasks;
        private readonly ConnectionSettings _settings;
        private CancellationTokenSource _ct;
        private ILoggerFactory _loggerFactory;
        private ArtemisReceiver receiver;

        public MessageReceiver(ConnectionSettings settings, ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _tasks = new ConcurrentBag<Task>();
            _ct = new CancellationTokenSource();
            _loggerFactory = loggerFactory;
        }

        public async Task Start(Func<Message, Task> messageHandler)
        {            
            receiver = new ArtemisReceiver(_settings, _loggerFactory);
            await receiver.Init(_ct.Token);
            var t = Task.Run(async() => await receiver.GetMessages(messageHandler, _ct.Token), _ct.Token);
            
            _tasks.Add(t);
        }

        public async Task StopAll()
        {
            _ct.Cancel();
            await Task.WhenAll(_tasks.ToArray()).ConfigureAwait(false);
            _ct.Dispose();

            _tasks = new ConcurrentBag<Task>();
            _ct = new CancellationTokenSource();        
            
           receiver.Dispose();
        }
    }
}
