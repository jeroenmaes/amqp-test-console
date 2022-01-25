using System;
using System.Configuration;
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
                
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
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
