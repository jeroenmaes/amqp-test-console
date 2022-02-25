using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AmqpTest
{
    internal class ArtemisReceiver : IDisposable
    {
        private IConsumer receiver;
        private ConnectionSettings _settings;
        private IConnection connection;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;

        public ArtemisReceiver(ConnectionSettings settings, ILoggerFactory loggerFactory = null)
        {
            _logger = ApplicationLogging.CreateLogger<ArtemisReceiver>();
            _settings = settings;            
            if (loggerFactory == null)
            {
                _loggerFactory = new NullLoggerFactory();
            }
            else
            {
                _loggerFactory = loggerFactory;
            }
        }

        public async Task Init(CancellationToken token)
        {
            var connectionFactory = new ConnectionFactory
            {
                LoggerFactory = _loggerFactory
            };

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

            connection = await connectionFactory.CreateAsync(endpoints, token);
           
            var address = "";
            var queue = "";
            if (_settings.ReceiveAddress.Contains("::"))
            {
                address = _settings.ReceiveAddress.Split(':')[0];
                queue = _settings.ReceiveAddress.Split(':')[2];
            }
            else
            {
                address = _settings.ReceiveAddress;
                queue = _settings.ReceiveAddress;
            }

            receiver = await connection.CreateConsumerAsync(new ConsumerConfiguration { Address = address, Queue = queue });
        }

        internal async Task GetMessages(Func<AmqpTest.Message, Task> messageHandler, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
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
                    }
                    catch (ConsumerClosedException e)
                    {
                        //Only message, ConnectionFactory will handle reconnect
                        _logger.LogWarning(e, "ConsumerClosedException");
                    }                    

                    token.ThrowIfCancellationRequested();

                    Thread.Sleep(100);
                }
            }
            catch (OperationCanceledException /*ex*/)
            {
                //ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected Exception");

                Dispose();
            }
        }

        public async void Dispose()
        {
            try
            {
                await connection.DisposeAsync();                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected Exception");
            }
        }
    }
}