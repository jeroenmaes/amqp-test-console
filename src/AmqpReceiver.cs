using Amqp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AmqpTestConsole
{
    internal class AmqpReceiver : IDisposable
    {
        
        private Connection connection;
        private Session session;
        private ReceiverLink receiver;

        public AmqpReceiver(ConnectionSettings settings) 
        {            
            string url = settings.ConnectionString;
            string source = settings.Queue;
            var appName = System.AppDomain.CurrentDomain.FriendlyName;
            Address peerAddr = new Address(url);

            connection = new Connection(peerAddr);
            session = new Session(connection);
            receiver = new ReceiverLink(session, appName, source);
        }

        internal void GetMessages(Func<Message, Task> messageHandler, CancellationToken token)
        {
            try
            {

                while (!token.IsCancellationRequested)
                {
                    var msg = receiver.Receive(Timeout.InfiniteTimeSpan);
                    
                    var properties = msg.ApplicationProperties;
                    if (properties != null)
                    {
                        if (properties["myContext"] != null)
                        {
                            var myValue = properties["myContext"].ToString();
                        }                        
                    }

                    if (msg == null)
                        return;

                    var messageId = msg.Properties.CorrelationId;
                    messageHandler(new Message { Body = msg.Body.ToString(), MessageId = messageId });

                    receiver.Accept(msg);
                    
                    token.ThrowIfCancellationRequested();

                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);

                Dispose();
            }          

        }

        public void Dispose()
        {
            receiver?.Close();
            session?.Close();
            connection?.Close();
        }
    }
}