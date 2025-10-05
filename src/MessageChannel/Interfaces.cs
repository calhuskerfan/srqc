using System;

namespace MessageChannel
{
    public interface IChannelReader : IDisposable
    {
        event EventHandler<MessageReceivedEventArgs> MessageReceived;
        event EventHandler<ConsumerEventArgs> Registered;
        event EventHandler<ConsumerEventArgs> Unregistered;
        event EventHandler<ShutdownEventArgs> Shutdown;
        void CloseConnection();
        void Connect();
        uint MessageCount();
        void AcknowledgeDelivery(IDeliveryTag deliveryTag);
    }


    public interface IChannelWriter : IDisposable
    {
        void PublishMessage(string message);
        void PublishMessage(byte[] message);
        bool ChannelAvailable();
    }


    public interface IDeliveryTag
    {
    }


    public interface IConnectionManager<T>
    {
    }
}
