using MessageChannel;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Producer
{
    public interface IApplication
    {
        void Run();
    }

    public class Application : IApplication
    {
        private readonly ILogger<Application> _logger;

        public Application(ILogger<Application> logger)
        {
            _logger = logger;
        }

        public void Run()
        {
            _logger.LogInformation("GO");

            string writerConfig = @"{'channelType': 'rabbit'}";

            //set up our channels
            IChannelWriter writer = ChannelFactory.GetChannelWriter(JObject.Parse(writerConfig));

            for (int i = 0; i < 10; i++)
            {

                writer.PublishMessage($"{i}");
            }
        }
    }
}
