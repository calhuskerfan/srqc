using MessageChannel;
using Newtonsoft.Json.Linq;
using srqc.domain;
using System.Text;

namespace service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IConduitConfig _conduitConfig;
        private readonly ILoggerFactory _loggerFactory;

        public Worker(
            ILogger<Worker> logger,
            IConduitConfig conduitConfig,
            IConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _conduitConfig = conduitConfig;
            _configuration = configuration;

            //temporary while we refactor service injection
            _loggerFactory = loggerFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExecuteAsync");

            string readerConfig = @"{'channelType': 'rabbit'}";
            string writerConfig = @"{'channelType': 'rabbit','properties':{'queueName':'out-queue'}}";

            //set up our channels
            IChannelReader reader = ChannelFactory.GetChannelReader(JObject.Parse(readerConfig));
            IChannelWriter writer = ChannelFactory.GetChannelWriter(JObject.Parse(writerConfig));

            IProcessingSystem processingContainer = new Conduit(
                _loggerFactory.CreateLogger<Conduit>(), 
                _conduitConfig);

            processingContainer.MessageReadyAtExitEvent += (object sender, MessageReadyEventArgs e) =>
            {
                _logger.LogInformation(e.Message.ToString());
                writer.PublishMessage(e.Message.ToString());
            };

            reader.MessageReceived += (model, ea) =>
            {
                IClaimCheck claimCheck = processingContainer.WaitForProcessingSlotAvailable();
                MessageIn mi = new() { Text = Encoding.UTF8.GetString(ea.Body) };
                _logger.LogInformation(mi.ToString());
                processingContainer.LoadMessage(claimCheck, mi);
            };

            reader.Connect();

            _logger.LogInformation("Running");

            stoppingToken.WaitHandle.WaitOne();
        }
    }
}
