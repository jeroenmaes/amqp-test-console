using System;

namespace AmqpTest
{
    public static class Logger
    {
        public static void LogMessage(string message)
        {
            Console.WriteLine($"{DateTime.UtcNow}::INFO::" + message);
        }

        public static void LogError(Exception ex)
        {
            Console.WriteLine($"{DateTime.UtcNow}::ERROR::" + ex.Message);
        }

        public static void LogWarning(Exception ex)
        {
            Console.WriteLine($"{DateTime.UtcNow}::WARN::" + ex.Message);
        }
    }
}
