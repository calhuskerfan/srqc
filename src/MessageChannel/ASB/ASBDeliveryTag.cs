using Azure.Messaging.ServiceBus;

namespace MessageChannel.ASB
{
    internal class ASBDeliveryTag : IDeliveryTag
    {
        public ProcessMessageEventArgs args = null;
    }
}
