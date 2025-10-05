using System;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json.Linq;

namespace MessageChannel.ASB
{
    public class ASBSettings
    {
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }

        public ServiceBusTransportType TransportType { get; set; } = ServiceBusTransportType.AmqpTcp;

        public static ASBSettings FromJSON(JObject settings)
        {
            ASBSettings ret = new ASBSettings();
            ret.ConnectionString = settings["connectionString"].ToString();

            return ret;
        }
    }
}
