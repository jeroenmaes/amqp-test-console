using Microsoft.Extensions.Logging;
using System;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AmqpTest
{
    public class MessageSender
    {
        private ConcurrentBag<Task> _tasks;
        private readonly ConnectionSettings _settings;
        private CancellationTokenSource _ct;
        private ILoggerFactory _loggerFactory;
        private ArtemisSender sender;

        public MessageSender(ConnectionSettings settings, ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _tasks = new ConcurrentBag<Task>();
            _ct = new CancellationTokenSource();
            _loggerFactory = loggerFactory;
        }
        public async Task Start()
        {

            sender = new ArtemisSender(_settings, _loggerFactory);
            await sender.Init(_ct.Token);
            var t = Task.Run(async() => await PutRandomMessages(sender, _ct.Token), _ct.Token);

            _tasks.Add(t);
        }

        private async Task PutRandomMessages(ArtemisSender sender, CancellationToken token)
        {
            var generator = new MessageGenerator();
            while (!token.IsCancellationRequested)
            {
                await sender.PutMessage(generator.RandomString(1024), Guid.NewGuid().ToString("N").ToUpper(), token);
                                
                await Task.Delay(100);
            }
        }

        public async Task StopAll()
        {
            _ct.Cancel();
            await Task.WhenAll(_tasks.ToArray()).ConfigureAwait(false);
            _ct.Dispose();

            _tasks = new ConcurrentBag<Task>();
            _ct = new CancellationTokenSource();

           sender.Dispose();
        }
    }
}
