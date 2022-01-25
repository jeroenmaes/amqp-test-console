using System;
using System.Configuration;
using System.Threading.Tasks;

namespace AmqpTestConsole
{
    public class Program
    {
        static ConnectionSettings settings = new ConnectionSettings();

        static void Main()
        {
            try
            {
                MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }            
        }

        static async Task MainAsync()
        {
            settings.ConnectionString = ConfigurationManager.AppSettings["connection"];
            settings.Queue = ConfigurationManager.AppSettings["queue"];
            //settings.Queue = ConfigurationManager.AppSettings["topic"];
            //settings.Queue = ConfigurationManager.AppSettings["subscription"];

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
            sender.Start(settings.Queue);            
        }

        private static void StartMessagePumps(MessageReceiver receiver)
        {
            receiver.Start(settings.Queue, ProcessMessage);            
        }

        private static Task ProcessMessage(Message message)
        {
            Logger.LogMessage($"ProcessMessage:: {message.MessageId} - {message.Body}");

            //Simulate processing            
            return Task.Delay(100);
        }
    }
}
