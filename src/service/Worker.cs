using MessageChannel;
using Newtonsoft.Json.Linq;
using srqc.domain;
using System.Text;

namespace service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IProcessingSystem _processingSystem;

        public Worker(
            ILogger<Worker> logger,
            IProcessingSystem processingSystem)
        {
            _logger = logger;
            _processingSystem = processingSystem;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExecuteAsync");

            string readerConfig = @"{'channelType': 'rabbit'}";
            string writerConfig = @"{'channelType': 'rabbit','properties':{'queueName':'out-queue'}}";

            //set up our channels
            IChannelReader reader = ChannelFactory.GetChannelReader(JObject.Parse(readerConfig));
            IChannelWriter writer = ChannelFactory.GetChannelWriter(JObject.Parse(writerConfig));

            _processingSystem.MessageReadyAtExitEvent += (object sender, MessageReadyEventArgs e) =>
            {
                _logger.LogInformation(e.Message.ToString());
                writer.PublishMessage(e.Message.ToString());
            };

            reader.MessageReceived += (model, ea) =>
            {
                IClaimCheck claimCheck = _processingSystem.WaitForProcessingSlotAvailable();
                MessageIn mi = new() { Text = Encoding.UTF8.GetString(ea.Body) };
                _logger.LogInformation(mi.ToString());
                _processingSystem.LoadMessage(claimCheck, mi);
            };

            reader.Connect();

            _logger.LogInformation("Running");

            stoppingToken.WaitHandle.WaitOne();

            _logger.LogInformation("Stop Processing System");

            _processingSystem.Stop();

            _logger.LogInformation("Done");
        }
    }
}
