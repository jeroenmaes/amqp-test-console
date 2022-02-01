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
                GetNetVersion();
                SetHighestTlsVersion();

                settings = new ConnectionSettings
                {
                    Server = ConfigurationManager.AppSettings["connection"],
                    Address = ConfigurationManager.AppSettings["address"]
                };

                Console.WriteLine($"*** Connection: '{settings.Server}'");
                Console.WriteLine($"*** Address: '{settings.Address}'");

                MainImplementation();
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

        private static void GetNetVersion()
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

        private static void SetHighestTlsVersion()
        {
            try
            { 
                //Setting TLS to 1.3
                Console.WriteLine("Setting TLS to 1.3 ...");
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)12288
                                                     | (SecurityProtocolType)3072
                                                     | (SecurityProtocolType)768
                                                     | SecurityProtocolType.Tls;
            }
            catch (NotSupportedException)
            {
                try
                {
                    //Setting TLS to 1.2
                    Console.WriteLine("Setting TLS to 1.2 ...");
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072
                                                         | (SecurityProtocolType)768
                                                         | SecurityProtocolType.Tls;
                }
                catch (NotSupportedException)
                {
                    try
                    {
                        //Setting TLS to 1.1
                        Console.WriteLine("Setting TLS to 1.1 ...");
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)768
                                                             | SecurityProtocolType.Tls;
                    }
                    catch (NotSupportedException)
                    {
                        //Setting TLS to 1.0
                        Console.WriteLine("Setting TLS to 1.0 ...");
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                    }
                }
            }
        }

        static void MainImplementation()
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
                        receiver.StopAll();
                    }
                    else
                    {
                        Console.WriteLine("Starting all message receivers...");
                        StartMessagePumps(receiver);
                    }

                    receiveStarted = !receiveStarted;
                }
                else if (keyInfo.KeyChar == 's')
                {
                    if (sendStarted)
                    {
                        Console.WriteLine("Stopping all message senders...");
                        sender.StopAll();
                    }
                    else
                    {
                        Console.WriteLine("Starting all message senders...");
                        StartMessageSenders(sender);
                    }

                    sendStarted = !sendStarted;
                }
            }
        }

        private static void StartMessageSenders(MessageSender sender)
        {
            sender.Start(settings.Address);
        }

        private static void StartMessagePumps(MessageReceiver receiver)
        {
            receiver.Start(settings.Address, ProcessMessage);
        }

        private static async Task ProcessMessage(Message message)
        {
            Logger.LogMessage($"ProcessMessage:: {message.MessageId} - {message.Body}");

            //Simulate processing            
            await Task.Delay(100);
        }
    }
}
