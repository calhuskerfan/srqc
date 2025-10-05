namespace MessageChannel.RabbitMQ
{
    public abstract class RMQChannel<T>
    {
        private readonly RMQSettings _settings;

        public RMQSettings Settings { get { return _settings; } }
        public bool Durable { get; set; } = true;
        public bool AutoAck { get; set; } = true;

        public RMQChannel(RMQSettings settings)
        {
            _settings = settings;
        }
    }
}
