using ActiveMQ.Artemis.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AmqpTest
{
    public abstract class ArtemisConnection : IDisposable
    {
        internal ILoggerFactory _loggerFactory;
        private ILogger _logger;
        internal ConnectionSettings _settings;
        internal ConnectionFactory _connectionFactory;
        internal List<Endpoint> _endpoints;
        internal IConnection _connection;

        public ArtemisConnection(ConnectionSettings settings, ILoggerFactory loggerFactory = null)
        {
            _logger = ApplicationLogging.CreateLogger<ArtemisConnection>();
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

        internal async Task Init(System.Threading.CancellationToken token)
        {
            _connectionFactory = new ConnectionFactory
            {
                LoggerFactory = _loggerFactory
            };

            Scheme schema = Scheme.Amqp;
            if (_settings.Protocol == "amqp")
                schema = Scheme.Amqp;
            else if (_settings.Protocol == "amqps")
                schema = Scheme.Amqps;

            _endpoints = new List<Endpoint>();

            if (!_settings.Servers.Contains(","))
            {
                var master = _settings.Servers;
                var masterServer = master.Split(':')[0];
                var masterPort = master.Split(':')[1];
                var masterEndpoint = Endpoint.Create(masterServer, int.Parse(masterPort), _settings.User, _settings.Password, schema);
                _endpoints.Add(masterEndpoint);

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
                _endpoints.Add(masterEndpoint);
                _endpoints.Add(slaveEndpoint);
            }

            _connection = await _connectionFactory.CreateAsync(_endpoints, token);
        }
        public async void Dispose()
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected Exception");
            }
        }
    }
}
