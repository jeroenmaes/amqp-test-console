
using ActiveMQ.Artemis.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AmqpTest
{
    internal class ArtemisSender : ArtemisConnection, IDisposable
    {

        private bool toggle;
        private IProducer producer;
        private ILogger _logger;

        public ArtemisSender(ConnectionSettings settings, ILoggerFactory loggerFactory = null) : base(settings, loggerFactory)
        {
            _logger = ApplicationLogging.CreateLogger<ArtemisSender>();
        }

        new public async Task Init(CancellationToken token)
        {
            await base.Init(token);

            producer = await _connection.CreateProducerAsync(new ProducerConfiguration
            {
                Address = _settings.SendAddress,
                MessageDurabilityMode = DurabilityMode.Durable
            });

        }

        internal async Task PutMessage(string message, string messageId, CancellationToken token)
        {
            try
            {
                var msg = new ActiveMQ.Artemis.Client.Message(message)
                {
                    CorrelationId = messageId,
                    CreationTime = DateTime.UtcNow
                };

                if (toggle == true)
                {
                    msg.ApplicationProperties["myContext"] = "TEST";
                    toggle = false;
                }
                else
                {
                    toggle = true;
                }

                await producer.SendAsync(msg, token);

                _logger.LogInformation($"SendMessage:: {messageId} - {message.Substring(0, 40)}...");
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
