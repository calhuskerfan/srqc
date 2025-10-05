using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageChannel.RabbitMQ
{
    public class RMQConsumer : RMQChannel<RMQConsumer>, IChannelReader
    {
        private IChannel _consumerChannel;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<ConsumerEventArgs> Registered;
        public event EventHandler<ConsumerEventArgs> Unregistered;
        public event EventHandler<ShutdownEventArgs> Shutdown;

        public RMQConsumer(RMQSettings init) : base(init)
        {
        }

        public uint MessageCount()
        {
            return _consumerChannel
                .MessageCountAsync(this.Settings.QueueName)
                .Result;
        }

        protected virtual void OnMessageReceived(byte[] body, ulong deliveryTag)
        {
            var mrea = new MessageReceivedEventArgs(body)
            {
                DeliveryTag = new RMQDeliveryTag(deliveryTag)
            };

            MessageReceived?.Invoke(this, mrea);
        }

        private IConnection GetConnection(RMQSettings settings)
        {
            return RMQConnectionManager
                .Instance
                .GetConnection(settings);
        }

        public void Connect()
        {
            _consumerChannel = GetConnection(this.Settings)
                .CreateChannelAsync()
                .Result;

            _consumerChannel.QueueDeclareAsync(
                queue: this.Settings.QueueName,
                durable: this.Durable,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_consumerChannel);

            consumer.ReceivedAsync += (model, ea) =>
            {
                this.OnMessageReceived(
                    ea.Body.ToArray(),
                    ea.DeliveryTag);

                return Task.CompletedTask;
            };

            consumer.RegisteredAsync += (model, ea) =>
            {
                return Task.CompletedTask;
            };

            consumer.UnregisteredAsync += (model, ea) =>
            {
                return Task.CompletedTask;
            };

            consumer.ShutdownAsync += (model, ea) =>
            {
                return Task.CompletedTask;
            };

            _consumerChannel.BasicConsumeAsync(
                queue: this.Settings.QueueName,
                autoAck: this.AutoAck,
                consumer: consumer);
        }

        public void AcknowledgeDelivery(IDeliveryTag deliveryTag)
        {
            if (!this.AutoAck && (deliveryTag is RMQDeliveryTag))
            {
                _consumerChannel.BasicAckAsync(
                    deliveryTag: (deliveryTag as RMQDeliveryTag).DeliveryTag,
                    multiple: false);
            }
        }

        public void CloseConnection()
        {
            _consumerChannel.CloseAsync();
        }

        public void Dispose()
        {
            //TBD
        }
    }
}
