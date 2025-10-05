namespace MessageChannel.RabbitMQ
{
    internal class RMQDeliveryTag : IDeliveryTag
    {
        public ulong DeliveryTag { get; set; }

        public RMQDeliveryTag(ulong deliveryTag)
        {
            this.DeliveryTag = deliveryTag;
        }
    }
}
