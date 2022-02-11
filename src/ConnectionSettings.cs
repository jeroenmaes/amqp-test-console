namespace AmqpTestConsole
{
    public class ConnectionSettings
    {
        public string Address { get; set; }
        public string Protocol { get; internal set; }
        public string Servers { get; internal set; }
        public string User { get; internal set; }
        public string Password { get; internal set; }
        public string Connection { get; internal set; }
    }
}