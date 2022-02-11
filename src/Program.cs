using Amqp;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.Net;
using System.Threading.Tasks;

namespace AmqpTestConsole
{
    public class Program
    {
        static ConnectionSettings settings;

        static void Main()
        {
            try
            {
                OutputNetVersion();
                
                //Disable cert validation --only for testing!
                //Connection.DisableServerCertValidation = true;

                settings = new ConnectionSettings
                {
                    Protocol = ConfigurationManager.AppSettings["protocol"],
                    Servers = ConfigurationManager.AppSettings["servers"],
                    User = ConfigurationManager.AppSettings["user"],
                    Password = ConfigurationManager.AppSettings["password"],
                    Address = ConfigurationManager.AppSettings["address"],
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
                Console.WriteLine($"*** Address: '{settings.Address}'");

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

        private static void OutputNetVersion()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    Console.WriteLine($".NET Framework Version: {CheckFor45PlusVersion((int)ndpKey.GetValue("Release"))}");
                }
                else
                {
                    Console.WriteLine(".NET Framework Version 4.5 or later is not detected.");
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
            await sender.Start(settings.Address);
        }

        private static async Task StartMessagePumps(MessageReceiver receiver)
        {
            await receiver.Start(settings.Address, ProcessMessage);
        }

        private static async Task ProcessMessage(Message message)
        {
            Logger.LogMessage($"ProcessMessage:: {message.MessageId} - {message.Body}");

            //Simulate processing            
            await Task.Delay(100);
        }
    }
}
