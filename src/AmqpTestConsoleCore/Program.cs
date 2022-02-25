using AmqpTest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Versioning;

namespace AmqpTestConsole
{
    public class Program
    {
        private static ILogger _logger;

        static void Main()
        {
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", true, true);

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ApplicationLogging.LoggerFactory = loggerFactory;
            _logger = ApplicationLogging.CreateLogger<Program>();

            var config = builder.Build();

            var framework = Assembly
            .GetEntryAssembly()?
            .GetCustomAttribute<TargetFrameworkAttribute>()?
            .FrameworkName;

            var stats = new
            {
                OsPlatform = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                AspDotnetVersion = framework
            };

            _logger.LogInformation($"OS: '{stats.OsPlatform}'");
            _logger.LogInformation($"Dotnet: '{stats.AspDotnetVersion}'");

            try
            {

                ConnectionSettings settings = new ConnectionSettings
                {
                    Protocol = config["protocol"],
                    Servers = config["servers"],
                    User = config["user"],
                    Password = config["password"],
                    SendAddress = config["send-address"],
                    ReceiveAddress = config["receive-address"],
                    Connection = ""
                };

                if (settings.Servers.Split(',').Length == 2)
                {
                    var servers = settings.Servers.Split(',');
                    settings.Connection = $"{settings.Protocol}://{settings.User}:{settings.Password}@{servers[0]},{settings.Protocol}://{settings.User}:{settings.Password}@{servers[1]}";
                }
                else if (settings.Servers.Split(',').Length == 1)
                {
                    settings.Connection = $"{settings.Protocol}://{settings.User}:{settings.Password}@{settings.Servers}";
                }
                else
                {
                    throw new Exception("Unexpected amount of servers");
                }
                _logger.LogInformation($"Connection: '{settings.Connection}'");
                _logger.LogInformation($"Send-Address: '{settings.SendAddress}'");
                _logger.LogInformation($"Receive-Address: '{settings.ReceiveAddress}'");


                MainImplementation(settings, loggerFactory).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected Exception");
                if (e.InnerException != null)
                    _logger.LogError(e.InnerException, "Inner Exception");

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        static async Task MainImplementation(ConnectionSettings settings, ILoggerFactory loggerFactory)
        {
            var receiver = new MessageReceiver(settings, loggerFactory);
            var sender = new MessageSender(settings, loggerFactory);

            var receiveStarted = false;
            var sendStarted = false;
            var keyInfo = new ConsoleKeyInfo();
            while (keyInfo.KeyChar != 'e' && keyInfo.KeyChar != 'E')
            {
                Console.WriteLine("Press <e> to Exit, <r> to stop/start receiving messages, <s> to stop/start sending messages");
                keyInfo = Console.ReadKey();
                if (keyInfo.KeyChar == 'r')
                {
                    if (receiveStarted)
                    {
                        _logger.LogInformation("Stopping all message receivers...");
                        await receiver.StopAll();
                    }
                    else
                    {
                        _logger.LogInformation("Starting all message receivers...");
                        await StartMessagePumps(receiver);
                    }

                    receiveStarted = !receiveStarted;
                }
                else if (keyInfo.KeyChar == 's')
                {
                    if (sendStarted)
                    {
                        _logger.LogInformation("Stopping all message senders...");
                        await sender.StopAll();
                    }
                    else
                    {
                        _logger.LogInformation("Starting all message senders...");
                        await StartMessageSenders(sender);
                    }

                    sendStarted = !sendStarted;
                }
            }
        }

        private static async Task StartMessageSenders(MessageSender sender)
        {
            await sender.Start();
        }

        private static async Task StartMessagePumps(MessageReceiver receiver)
        {
            await receiver.Start(ProcessMessage);
        }

        private static async Task ProcessMessage(Message message)
        {            
            _logger.LogInformation($"ProcessMessage:: {message.MessageId} - {message.Body.Substring(0, 40)}...");

            //Simulate processing            
            await Task.Delay(100);
        }
    }
}
