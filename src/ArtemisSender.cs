
using ActiveMQ.Artemis.Client;
using System;
using System.Threading.Tasks;

namespace AmqpTestConsole
{
    internal class ArtemisSender : IDisposable
    {        

        private bool toggle;
        private IProducer producer;
        private IConnection connection;

        public ArtemisSender(ConnectionSettings settings) 
        {            
            string url = settings.Connection;
            string target = settings.Address;
            var appName = System.AppDomain.CurrentDomain.FriendlyName;

        }

        public async Task Init()
        {
            var connectionFactory = new ConnectionFactory();
            
            var masterEndpoint = Endpoint.Create("localhost", 61616, "artemis", "simetraehcapa", Scheme.Amqp);
            var slaveEndpoint = Endpoint.Create("localhost", 61616, "artemis", "simetraehcapa", Scheme.Amqp);

            connection = await connectionFactory.CreateAsync(new[]
            {
                masterEndpoint,
                slaveEndpoint
            });
            connection.ConnectionRecoveryError += (sender, eventArgs) =>
            {
                Logger.LogMessage("Producer Connection Error:" + eventArgs.Exception.Message);
            };

            producer = await connection.CreateProducerAsync(new ProducerConfiguration
            {
                Address = "test.topic",
                //RoutingType = RoutingType.Anycast,
                MessageDurabilityMode = DurabilityMode.Durable
            });

        }

        internal async Task PutMessage(string message, string messageId)
        {
            try
            {
                var msg = new ActiveMQ.Artemis.Client.Message(message);
                msg.CorrelationId = messageId;                
                msg.CreationTime = DateTime.UtcNow;                
                
                if (toggle == true)
                {                    
                    msg.ApplicationProperties["myContext"] = "TEST";
                    toggle = false;
                }
                else
                { 
                    toggle = true;
                }
                                                
                await producer.SendAsync(msg);
                Logger.LogMessage($"SendMessage:: {messageId} - {message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                
                Dispose();
            }                     
        }
        
        public async void Dispose()
        {
            try
            {
                await connection.DisposeAsync();
                await producer.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }           
        }
    }
}
