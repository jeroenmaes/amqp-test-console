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
        private AmqpSender sender;

        public MessageSender(ConnectionSettings settings)
        {
            _settings = settings;
            _tasks = new ConcurrentBag<Task>();
            _ct = new CancellationTokenSource();
        }
        public void Start(string queueName)
        {

            sender = new AmqpSender(_settings);
            var t = Task.Run(() => PutRandomMessages(sender, _ct.Token), _ct.Token);

            _tasks.Add(t);
        }

        private void PutRandomMessages(AmqpSender sender, CancellationToken token)
        {
            var generator = new MessageGenerator();
            while (!token.IsCancellationRequested)
            {
                sender.PutMessage(generator.RandomString(1024), Guid.NewGuid().ToString("N").ToUpper());

                Thread.Sleep(100);
            }
        }

        public void StopAll()
        {
            _ct.Cancel();
            Task.WhenAll(_tasks.ToArray()).ConfigureAwait(false);
            _ct.Dispose();

            _tasks = new ConcurrentBag<Task>();
            _ct = new CancellationTokenSource();

            sender.Dispose();
        }
    }
}
