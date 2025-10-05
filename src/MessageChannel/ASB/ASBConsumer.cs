using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using System;
using System.Threading.Tasks;

namespace MessageChannel.ASB
{
    public class ASBConsumer : ASBChannel<ASBConsumer>, IChannelReader
    {
        public bool AutoCompleteMessages { get; set; } = true;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<ConsumerEventArgs> Registered;

        public event EventHandler<ConsumerEventArgs> Unregistered;

        public event EventHandler<ShutdownEventArgs> Shutdown;

        private ServiceBusClient client;
        private ServiceBusProcessor processor;

        public ASBConsumer(ASBSettings init)
            : base(init)
        {
        }

        async Task MessageHandler(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body.ToArray();

            ASBDeliveryTag t = new ASBDeliveryTag
            {
                args = args
            };

            MessageReceivedEventArgs mrea = new MessageReceivedEventArgs(body)
            {
                DeliveryTag = t
            };

            MessageReceived?.Invoke(this, mrea);
        }

        Task ErrorHandler(ProcessErrorEventArgs args)
        {
            return Task.CompletedTask;
        }

        public async void Connect()
        {
            var sbcOptions = new ServiceBusClientOptions
            {
                TransportType = this.Settings.TransportType
            };

            var sbpOptions = new ServiceBusProcessorOptions()
            {
                AutoCompleteMessages = this.AutoCompleteMessages
            };

            if(!this.QueueExists())
            {
                this.CreateQueue();
            }

            client = this.GetServiceBusClient(this.Settings.ConnectionString, sbcOptions);
            processor = client.CreateProcessor(this.Settings.QueueName, sbpOptions);
            processor.ProcessMessageAsync += this.MessageHandler;
            processor.ProcessErrorAsync += this.ErrorHandler;
            await processor.StartProcessingAsync();
        }

        private ServiceBusClient GetServiceBusClient(string connectionString, ServiceBusClientOptions sbcOptions)
        {
            return ConnectionManager.Instance.GetServiceBusClient(connectionString, sbcOptions);
        }

        public void CloseConnection()
        {
            processor.CloseAsync();
        }

        public void Dispose()
        {
            //TBD
        }
        
        public void AcknowledgeDelivery(IDeliveryTag deliveryTag)
        {
            if (!processor.AutoCompleteMessages && (deliveryTag is ASBDeliveryTag))
            {
                (deliveryTag as ASBDeliveryTag).args.CompleteMessageAsync((deliveryTag as ASBDeliveryTag).args.Message);
            }
        }

        public uint MessageCount()
        {
            var client = new ServiceBusAdministrationClient(this.Settings.ConnectionString);
            var runtimeProps = client.GetQueueRuntimePropertiesAsync(this.Settings.QueueName).Result.Value;
            return (uint)runtimeProps.ActiveMessageCount;
        }
    }
}
