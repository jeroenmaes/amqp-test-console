namespace AmqpTest
{
    public class ConnectionSettings
    {
        public string ReceiveAddress { get; set; }
        public string SendAddress { get; set; }

        public string Protocol { get;  set; }
        public string Servers { get;  set; }
        public string User { get;  set; }
        public string Password { get;  set; }
        public string Connection { get;  set; }
        public string SendBatchSize { get; set; }
    }
}