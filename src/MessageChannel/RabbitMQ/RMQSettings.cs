using System;
using MessageChannel.ASB;
using Newtonsoft.Json.Linq;

namespace MessageChannel.RabbitMQ
{
    public class RMQSettings
    {
        public string UserName { get; set; } = "guest";

        public string Password { get; set; } = "guest";

        public string HostName { get; set; } = "localhost";

        public int Port { get; set; } = 5672;

        public string VHost { get; set; } = "/";

        public string QueueName { get; set; } = "default-queue";

        //leave as is for now, want to work on this one a little
        public string RoutingKey { get; set; } = "default-queue";

        public static RMQSettings FromJSON(JObject settings)
        {
            RMQSettings retval = new RMQSettings();

            if (settings != null && settings["queueName"] != null)
            {
                retval.QueueName = settings["queueName"].Value<string>();
                retval.RoutingKey = settings["queueName"].Value<string>();
            }

            return retval;
        }
    }
}
