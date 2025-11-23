using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Srqc;
using System.Diagnostics;

namespace console
{
    public interface IApplication
    {
        void Run();
    }

#pragma warning disable CS8622

    public class Application : IApplication
    {
        internal static Random r = new();

        private readonly ILogger<Application> _logger;
        private readonly IConfiguration _configuration;
        private readonly ConduitConfig _conduitConfig;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IProcessingSystem _processingSystem;


        public Application(ILogger<Application> logger,
            IOptions<ConduitConfig> options,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IProcessingSystem processingSystem)
        {
            _loggerFactory = loggerFactory;
            _logger = logger;
            _conduitConfig = options.Value;
            _configuration = configuration;
            _processingSystem = processingSystem;
        }


        /// <summary>
        /// Load Inbound Messages Builds a set of test messages.
        /// </summary>
        /// <remarks>
        /// Case 0: the default, based on appParams
        /// Case 1: demonstrates a simple example where pod 1 finishes and starts message 4 while pods 2 and 3 are still running.
        /// Case 1: demonstrates improvement possibilities by processing all five messages in little more than the 'longest' pole at message 3
        /// </remarks>
        /// <param name="inboundMessages"></param>
        /// <param name="appParams"></param>
        /// <param name="TestScenario"></param>
        public static void CreateInboundMessages(ref ApplicationContext ctx)
        {
            switch (ctx.appParams.TestScenario)
            {
                case 1:
                    ctx.inboundMessages.Add(new MessageIn() { Id = 1, Text = "1", ProcessingMsec = 100 });
                    ctx.inboundMessages.Add(new MessageIn() { Id = 2, Text = "2", ProcessingMsec = 500 });
                    ctx.inboundMessages.Add(new MessageIn() { Id = 3, Text = "3", ProcessingMsec = 1000 });
                    ctx.inboundMessages.Add(new MessageIn() { Id = 4, Text = "4", ProcessingMsec = 100 });
                    break;
                case 2:
                    ctx.inboundMessages.Add(new MessageIn() { Id = 1, Text = "1", ProcessingMsec = 100 });
                    ctx.inboundMessages.Add(new MessageIn() { Id = 2, Text = "2", ProcessingMsec = 700 });
                    ctx.inboundMessages.Add(new MessageIn() { Id = 3, Text = "3", ProcessingMsec = 1000 });
                    ctx.inboundMessages.Add(new MessageIn() { Id = 4, Text = "4", ProcessingMsec = 900 });
                    ctx.inboundMessages.Add(new MessageIn() { Id = 5, Text = "5", ProcessingMsec = 100 });
                    break;
                default:
                    {
                        for (int i = 1; i < ctx.appParams.MessageCount + 1; i++)
                        {
                            ctx.inboundMessages.Add(new MessageIn()
                            {
                                Id = i,
                                Text = i.ToString(),
                                ProcessingMsec = r.Next(ctx.appParams.MinProcessingDelay, ctx.appParams.MaxProcessingDelay)
                            });
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Runs the application
        /// </summary>
        public void Run()
        {
            _logger.LogInformation("Run Application");

            Stopwatch totalProcessingTime = Stopwatch.StartNew();

            _logger.LogDebug("Start Loading");

            ApplicationContext ctx = new ApplicationContext
            {
                appParams = new()
                {
                    MessageCount = Convert.ToInt32(_configuration["AppSettings:MessageCount"]),
                    MinProcessingDelay = Convert.ToInt32(_configuration["AppSettings:MinProcessingDelay"]),
                    MaxProcessingDelay = Convert.ToInt32(_configuration["AppSettings:MaxProcessingDelay"]),
                    TestScenario = Convert.ToInt32(_configuration["AppSettings:TestScenario"])
                }
            };

            console.Application.CreateInboundMessages(ref ctx);

            // register for messages ready at exit
            _processingSystem.MessageReadyAtExitEvent += (object sender, MessageReadyEventArgs e) =>
            {
                ctx.outboundMessages.Add(e.Message);
            };

            // send messages into processing system
            for (int i = 0; i < ctx.inboundMessages.Count; i++)
            {
                IClaimCheck claimCheck = _processingSystem.WaitForProcessingSlotAvailable();
                _processingSystem.LoadMessage(claimCheck, ctx.inboundMessages[i]);
            }

            _logger.LogDebug("Loading Completed");

            _processingSystem.Stop();

            totalProcessingTime.Stop();

            _logger.LogInformation("Container Finished Processing");

            ctx.RunQualityCheck(_conduitConfig.PodCount, logindividual: true);

            _logger.LogInformation($"Total processing time: {totalProcessingTime.Elapsed.TotalMilliseconds} msec.  Accumulated 'Serial' Time: {ctx.AccumulatedMsec} msec.  Ratio: {ctx.AccumulatedMsec / totalProcessingTime.Elapsed.TotalMilliseconds}");
        }
    }
#pragma warning restore CS8622
}
