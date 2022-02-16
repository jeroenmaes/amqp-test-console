using System;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AmqpTestConsole
{
    public class MessageSender
    {
        private ConcurrentBag<Task> _tasks;
        private readonly ConnectionSettings _settings;
        private CancellationTokenSource _ct;
        private ArtemisSender sender;

        public MessageSender(ConnectionSettings settings)
        {
            _settings = settings;
            _tasks = new ConcurrentBag<Task>();
            _ct = new CancellationTokenSource();
        }
        public async Task Start()
        {

            sender = new ArtemisSender(_settings);
            await sender.Init();
            var t = Task.Run(async() => await PutRandomMessages(sender, _ct.Token), _ct.Token);

            _tasks.Add(t);
        }

        private async Task PutRandomMessages(ArtemisSender sender, CancellationToken token)
        {
            var generator = new MessageGenerator();
            while (!token.IsCancellationRequested)
            {
                await sender.PutMessage(generator.RandomString(1024), Guid.NewGuid().ToString("N").ToUpper());
                                
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
