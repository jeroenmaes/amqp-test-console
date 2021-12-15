using Amqp;
using Amqp.Framing;
using System;

namespace AmqpTestConsole
{
    internal class AmqpSender : IDisposable
    {        
        private Connection connection;
        private Session session;
        private SenderLink sender;

        public AmqpSender(ConnectionSettings settings) 
        {            
            string url = settings.ConnectionString;
            string target = settings.Queue;
            var appName = System.AppDomain.CurrentDomain.FriendlyName;

            Address peerAddr = new Address(url);
            connection = new Connection(peerAddr);
            session = new Session(connection);
            sender = new SenderLink(session, appName, target);
        }
                
        internal void PutMessage(string message, string messageId)
        {
            try
            {
                var msg = new Amqp.Message(message);
                msg.Properties = new Amqp.Framing.Properties();
                msg.Properties.SetCorrelationId(messageId);
                msg.Properties.CreationTime = DateTime.UtcNow;
                msg.Header = new Header() { Durable = true };

                sender.Send(msg);
                Logger.LogMessage($"SendMessage:: {messageId} - {message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                
                Dispose();
            }         
            
        }

        public void Dispose()
        {
            sender?.Close();
            session?.Close();
            connection?.Close();
        }
    }
}