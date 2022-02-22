
using ActiveMQ.Artemis.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AmqpTest
{
    internal class ArtemisSender : IDisposable
    {

        private bool toggle;
        private IProducer producer;
        private IConnection connection;
        private ConnectionSettings _settings;

        public ArtemisSender(ConnectionSettings settings)
        {
            _settings = settings;
            var appName = System.AppDomain.CurrentDomain.FriendlyName;
        }

        public async Task Init()
        {
            var connectionFactory = new ConnectionFactory();
            connectionFactory.LoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            Scheme schema = Scheme.Amqp;
            if (_settings.Protocol == "amqp")
                schema = Scheme.Amqp;
            else if (_settings.Protocol == "amqps")
                schema = Scheme.Amqps;

            var endpoints = new List<Endpoint>();

            if (!_settings.Servers.Contains(","))
            {
                var master = _settings.Servers;
                var masterServer = master.Split(':')[0];
                var masterPort = master.Split(':')[1];
                var masterEndpoint = Endpoint.Create(masterServer, int.Parse(masterPort), _settings.User, _settings.Password, schema);
                endpoints.Add(masterEndpoint);

            }
            else
            {

                var master = _settings.Servers.Split(',')[0];
                var masterServer = master.Split(':')[0];
                var masterPort = master.Split(':')[1];

                var slave = _settings.Servers.Split(',')[1];
                var slaveServer = slave.Split(':')[0];
                var slavePort = slave.Split(':')[1];

                var masterEndpoint = Endpoint.Create(masterServer, int.Parse(masterPort), _settings.User, _settings.Password, schema);
                var slaveEndpoint = Endpoint.Create(slaveServer, int.Parse(slavePort), _settings.User, _settings.Password, schema);
                endpoints.Add(masterEndpoint);
                endpoints.Add(slaveEndpoint);
            }

            connection = await connectionFactory.CreateAsync(endpoints);
            connection.ConnectionRecoveryError += (sender, eventArgs) =>
            {
                Logger.LogMessage("Producer Connection Error:" + eventArgs.Exception.Message);
            };

            producer = await connection.CreateProducerAsync(new ProducerConfiguration
            {
                Address = _settings.SendAddress,
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
