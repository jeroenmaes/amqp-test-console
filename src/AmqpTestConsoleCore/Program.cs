using Amqp;
using AmqpTest;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.Net;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace AmqpTestConsole
{
    public class Program
    {
        static ConnectionSettings settings;

        static void Main()
        {

            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true);

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

            Console.WriteLine($"-- OS: '{stats.OsPlatform}'");
            Console.WriteLine($"-- Dotnet : '{stats.AspDotnetVersion}'");

            try
            {
                
                settings = new ConnectionSettings
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



                Console.WriteLine($"*** Connection: '{settings.Connection}'");
                Console.WriteLine($"*** Send-Address: '{settings.SendAddress}'");
                Console.WriteLine($"*** Receive-Address: '{settings.ReceiveAddress}'");


                MainImplementation().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected Exception: " + e.Message);
                if (e.InnerException != null)
                    Console.WriteLine("Inner Exception: " + e.InnerException.Message);

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        static async Task MainImplementation()
        {
            var receiver = new MessageReceiver(settings);
            var sender = new MessageSender(settings);

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
            Logger.LogMessage($"ProcessMessage:: {message.MessageId} - {message.Body}");

            //Simulate processing            
            await Task.Delay(100);
        }
    }
}
