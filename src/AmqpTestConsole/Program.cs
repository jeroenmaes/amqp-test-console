using AmqpTest;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace AmqpTestConsole
{
    public class Program
    {
        private static ILogger _logger;

        static void Main()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ApplicationLogging.LoggerFactory = loggerFactory;
            _logger = ApplicationLogging.CreateLogger<Program>();

            try
            {
                OutputNetVersion();
                                
                ConnectionSettings settings = new ConnectionSettings
                {
                    Protocol = ConfigurationManager.AppSettings["protocol"],
                    Servers = ConfigurationManager.AppSettings["servers"],
                    User = ConfigurationManager.AppSettings["user"],
                    Password = ConfigurationManager.AppSettings["password"],
                    SendAddress = ConfigurationManager.AppSettings["send-address"],
                    ReceiveAddress = ConfigurationManager.AppSettings["receive-address"],
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

        private static void OutputNetVersion()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    _logger.LogInformation($".NET Framework Version: {CheckFor45PlusVersion((int)ndpKey.GetValue("Release"))}");
                }
                else
                {
                    _logger.LogInformation(".NET Framework Version 4.5 or later is not detected.");
                }
            }            
        }                
        private static string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 528040)
                return "4.8 or later";
            if (releaseKey >= 461808)
                return "4.7.2";
            if (releaseKey >= 461308)
                return "4.7.1";
            if (releaseKey >= 460798)
                return "4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
                return "4.6.1";
            if (releaseKey >= 393295)
                return "4.6";
            if (releaseKey >= 379893)
                return "4.5.2";
            if (releaseKey >= 378675)
                return "4.5.1";
            if (releaseKey >= 378389)
                return "4.5";
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
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
                        Console.WriteLine("Stopping all message receivers...");
                        await receiver.StopAll();
                    }
                    else
                    {
                        Console.WriteLine("Starting all message receivers...");
                        await StartMessagePumps(receiver);
                    }

                    receiveStarted = !receiveStarted;
                }
                else if (keyInfo.KeyChar == 's')
                {
                    if (sendStarted)
                    {
                        Console.WriteLine("Stopping all message senders...");
                        await sender.StopAll();
                    }
                    else
                    {
                        Console.WriteLine("Starting all message senders...");
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

        private static async Task StartMessagePumps(AmqpTest.MessageReceiver receiver)
        {
            await receiver.Start(ProcessMessage);
        }

        private static async Task ProcessMessage(AmqpTest.Message message)
        {            
            _logger.LogInformation($"ProcessMessage:: {message.MessageId} - {message.Body.Substring(0, 40)}...");

            //Simulate processing            
            await Task.Delay(100);
        }
    }
}
