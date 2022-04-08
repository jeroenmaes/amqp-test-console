using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AmqpTest
{
    internal class ArtemisReceiver : ArtemisConnection, IDisposable
    {
        private IConsumer receiver;
        private ILogger _logger;
        private readonly object statisticsLock = new object();

        public ArtemisReceiver(ConnectionSettings settings, ILoggerFactory loggerFactory = null) : base(settings, loggerFactory)
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

        new public async Task Init(CancellationToken token)
        {
            await base.Init(token);

            var address = "";
            var queue = "";
            //RoutingType routingType;
            if (_settings.ReceiveAddress.Contains("::"))
            {
                address = _settings.ReceiveAddress.Split(':')[0];
                queue = _settings.ReceiveAddress.Split(':')[2];
                //routingType = RoutingType.Multicast;
            }
            else
            {
                address = _settings.ReceiveAddress;
                queue = _settings.ReceiveAddress;
                //routingType = RoutingType.Anycast;
            }

            receiver = await _connection.CreateConsumerAsync(
                new ConsumerConfiguration
                {
                    Address = address,
                    Queue = queue,
                    Durable = true,
                    //RoutingType = routingType
                });

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
                        lock (statisticsLock)
                        {
                            MessageStatistics.TotalReceivedMessages++;
                        }
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

                base.Dispose();
            }
        }
    }
}