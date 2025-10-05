using MessageChannel.ASB;
using MessageChannel.RabbitMQ;
using Newtonsoft.Json.Linq;
using System;

namespace MessageChannel
{
    public class ChannelFactory
    {
        public static readonly string RabbitMQChannelTypeDefinition = "rabbit";
        public static readonly string ASBChannelTypeDefinition = "asb";
        public static readonly string AzureQueueStorageChannelTypeDefinition = "aqs";

        public static IChannelWriter GetChannelWriter(JObject definition)
        {
            if (definition["channelType"].Value<string>() == RabbitMQChannelTypeDefinition)
            {
                return new RMQPublisher(RMQSettings.FromJSON(definition["properties"] as JObject));
            }

            if (definition["channelType"].Value<string>() == ASBChannelTypeDefinition )
            {
                return new ASBPublisher(ASBSettings.FromJSON(definition["properties"] as JObject));
            }

            throw new ArgumentException();
        }

        public static IChannelReader GetChannelReader(JObject definition)
        {
            if (definition["channelType"].Value<string>() == "rabbit")
            {
                return new RMQConsumer(RMQSettings.FromJSON(definition["properties"] as JObject));
            }

            if (definition["channelType"].Value<string>() == "azure")
            {
                return new ASBConsumer(ASBSettings.FromJSON(definition["properties"] as JObject));
            }

            throw new ArgumentException();
        }

        public static T GetConnection<T>(JObject definition)
        {
            T c = definition["properties"].ToObject<T>();
            return c;
        }
    }
}
