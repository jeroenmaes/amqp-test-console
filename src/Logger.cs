using System;

namespace AmqpTestConsole
{
    public static class Logger
    {
        public static void LogMessage(string message)
        {
            Console.WriteLine($"{DateTime.UtcNow}::" + message);
        }

        public static void LogError(Exception ex)
        {
            Console.WriteLine($"{DateTime.UtcNow}::ERROR::" + ex.Message);
        }
    }
}
