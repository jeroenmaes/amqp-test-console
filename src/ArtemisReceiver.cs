using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client;


namespace AmqpTestConsole
{
    internal class ArtemisReceiver : IDisposable
    {
        private IConsumer receiver;
        private ConnectionSettings _settings;
        private IConnection connection;

        public ArtemisReceiver(ConnectionSettings settings) 
        {
            _settings = settings;
            string url = settings.Connection;
            string source = settings.Address;
            var appName = System.AppDomain.CurrentDomain.FriendlyName;
                              
        }

        public async Task Init()
        {
            var connectionFactory = new ConnectionFactory();

            //single endpoint
            //var endpoint = Endpoint.Create("localhost", 61616, "artemis", "simetraehcapa");

            var masterEndpoint = Endpoint.Create("localhost", 61616, "artemis", "simetraehcapa", Scheme.Amqp);
            var slaveEndpoint = Endpoint.Create("localhost", 61616, "artemis", "simetraehcapa", Scheme.Amqp);

            connection = await connectionFactory.CreateAsync(new[]
            {
                masterEndpoint,
                slaveEndpoint
            });            
            connection.ConnectionRecoveryError += (sender, eventArgs) =>
            {
                Logger.LogMessage("Consumer Connection Error:" + eventArgs.Exception.Message);
            };


            receiver = await connection.CreateConsumerAsync(new ConsumerConfiguration { Address = "test.topic", Queue ="test.topic1" });
        }

        internal async Task GetMessages(Func<Message, Task> messageHandler, CancellationToken token)
        {
            try
            {

                while (!token.IsCancellationRequested)
                {
                    var msg = await receiver.ReceiveAsync(token);
                    if (msg == null)
                        return;

                    var properties = msg.ApplicationProperties;
                    if (properties != null)
                    {
                        if (properties["myContext"] != null)
                        {
                            var myValue = properties["myContext"].ToString();
                        }                        
                    }
                    
                    var messageId = msg.CorrelationId;
                    await messageHandler(new Message { Body = msg.GetBody<string>().ToString(), MessageId = messageId });

                    await receiver.AcceptAsync(msg);
                    
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

        public async void Dispose()
        {
            try
            {
                await connection.DisposeAsync();
                await receiver.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }            
        }
    }
}