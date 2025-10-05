using System.Text;
using RabbitMQ.Client;

namespace MessageChannel.RabbitMQ
{
    public class RMQPublisher : RMQChannel<RMQPublisher>, IChannelWriter
    {

        private IChannel _channel = null;

        public RMQPublisher(RMQSettings settings) 
            : base(settings)
        {

        }

        public void PublishMessage(string message)
        {
            PublishMessage(Encoding.UTF8.GetBytes(message));
        }

        public void PublishMessage(byte[] message)
        {
            GetChannel().BasicPublishAsync(
                exchange: "",
                routingKey: this.Settings.RoutingKey,
                body: message);
        }

        private IConnection GetConnection(RMQSettings settings)
        {
            return RMQConnectionManager
                .Instance
                .GetConnection(settings);
        }

        public IChannel GetChannel()
        {
            if (_channel == null)
            {
                var channel = GetConnection(this.Settings)
                    .CreateChannelAsync()
                    .Result;

                channel.QueueDeclareAsync(
                    queue: this.Settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _channel = channel;
            }

            return _channel;
        }

        public void Dispose()
        {
            //TBD
        }

        bool IChannelWriter.ChannelAvailable()
        {
            ///TODOCJH:  Is there a better way to do this,
            ///should we keep some type of internal 'channel open attempts, etc'
            ///we must keep trying and fail gracefully
            if (_channel == null)
            {
                GetChannel();
            }

            if (_channel != null 
                && _channel.IsOpen)
            {
                return true;
            }

            return false;
        }
    }
}
