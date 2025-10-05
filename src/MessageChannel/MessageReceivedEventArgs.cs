using System;

namespace MessageChannel
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public byte[] Body { get; }
        public string ConsumerTag { get; set; }
        public IDeliveryTag DeliveryTag { get; set; }
        public string Exchange { get; set; }
        public bool Redelivered { get; set; }
        public string RoutingKey { get; set; }

        public MessageReceivedEventArgs(byte[] body)
        {
            this.Body = body;
        }
    }
}
