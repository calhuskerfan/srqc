using System.Text;
using Azure.Messaging.ServiceBus;

namespace MessageChannel.ASB
{
    public class ASBPublisher : ASBChannel<ASBPublisher>,  IChannelWriter
    {
        ServiceBusSender _sender = null;

        public ASBPublisher(ASBSettings init)
            : base(init)
        {
        }

        public void PublishMessage(string message)
        {
            PublishMessage(Encoding.UTF8.GetBytes(message));
        }

        public void PublishMessage(byte[] message)
        {
            if (_sender == null || _sender.IsClosed)
            {
                var sbcOptions = new ServiceBusClientOptions
                {
                    TransportType = this.Settings.TransportType
                };

                if (!this.QueueExists())
                {
                    this.CreateQueue();
                }

                _sender = this
                    .GetServiceBusClient(this.Settings.ConnectionString, sbcOptions)
                    .CreateSender(this.Settings.QueueName);
            }

            // The wait here is probably not required
            // just want to verify that there are no out of
            // consequences

            _sender.SendMessageAsync(new ServiceBusMessage(message)).Wait();
        }

        private ServiceBusClient GetServiceBusClient(string connectionString, ServiceBusClientOptions sbcOptions)
        {
            return ConnectionManager
                .Instance
                .GetServiceBusClient(connectionString, sbcOptions);
        }
        public void Dispose()
        {
            _sender.CloseAsync();
        }

        bool IChannelWriter.ChannelAvailable()
        {
            return true;
        }
    }
}
