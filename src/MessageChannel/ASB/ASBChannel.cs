using System.Threading.Tasks;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace MessageChannel.ASB
{
    public abstract class ASBChannel<T>
    {
        private readonly ASBSettings _settings;

        public ASBChannel()
        {
        }

        public ASBChannel(ASBSettings settings)
        {
            _settings = settings;
        }

        public ASBSettings Settings {
            get { return _settings; }
        }

        public bool QueueExists()
        {
            var qe = GetClient()
                .QueueExistsAsync(_settings.QueueName)
                .Result;

            return qe.Value;
        }

        public QueueProperties CreateQueue()
        {
            var qp = GetClient()
                .CreateQueueAsync(_settings.QueueName)
                .Result;

            return qp.Value;
        }

        public Task<Response<QueueProperties>> CreateQueueAsync()
        {
            return GetClient()
                .CreateQueueAsync(_settings.QueueName);
        }

        public string GetServiceBusFullyQualifiedNamespace(string connectionString)
        {
            return ServiceBusConnectionStringProperties
                .Parse(connectionString)
                .FullyQualifiedNamespace;
        }

        private ServiceBusAdministrationClient GetClient()
        {
            return new ServiceBusAdministrationClient(_settings.ConnectionString);
        }
    }
}
