using MessageChannel;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer
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

        public void Run() {

            _logger.LogInformation("GO");

            var exitEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            string readerConfig = @"{'channelType': 'rabbit','properties':{'queueName':'out-queue'}}";

            IChannelReader reader = ChannelFactory.GetChannelReader(JObject.Parse(readerConfig));

            reader.MessageReceived += (model, ea) =>
            {
                _logger.LogInformation(Encoding.UTF8.GetString(ea.Body));
            };

            reader.Connect();

            _logger.LogInformation("Connected Ctrl+C to exit");

            exitEvent.WaitOne();

            reader.CloseConnection();
        }
    }
}
