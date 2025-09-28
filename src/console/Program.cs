using Serilog;
using srqc;
using srqc.domain;
using System.Diagnostics;

Log.Logger = new LoggerConfiguration()
  .Enrich.WithThreadId()
  .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {ThreadId,3} {Message:lj}{NewLine}{Exception}")
  .MinimumLevel.Information()
  .CreateLogger();

ILogger _logger = Log.Logger;

_logger.Information("Starting");

ApplicationParameters appParams = new()
{
    PodCount = 3,
    MessageCount = 13,
    MinProcessingDelay = 75,
    MaxProcessingDelay = 125,
};


// inbound and outbound message 'queues'
List<MessageIn> inboundMessages = [];
List<MessageOut> outboundMessages = [];


//update to run specific scenarios.  see LoadInboundMessages for details
int testCase = 0;

console.Application.LoadInboundMessages(
    ref inboundMessages,
    ref appParams,
    testCase);

var processingContainer = console.Application.GetProcessingContainer(appParams);
Random r = new();

// exit handler
processingContainer.MessageReadyAtExitEvent += (object sender, MessageReadyEventArgs e) =>
{
    outboundMessages.Add(e.Message);
};


// overall timer
Stopwatch totalProcessingTime = Stopwatch.StartNew();

_logger.Information("Start Loading");

// start sending messages into the queue
for (int i = 0; i < inboundMessages.Count; i++)
{
    IClaimCheck claimCheck = processingContainer.WaitForProcessingSlotAvailable();
    processingContainer.LoadMessage(claimCheck, inboundMessages[i]);
}

_logger.Information("Done Loading");

processingContainer.Stop();
totalProcessingTime.Stop();

_logger.Information("Container Finished Processing");

// accumulate a rough estimate of what the 'serial' message processing time would have been
int accumulatedMsec = 0;

// set logindividual to true if you not want to see all the message results at the end
RunQualityCheck(logindividual: true);

_logger.Information($"Total processing time: {totalProcessingTime.Elapsed.TotalMilliseconds} msec.  Accumulated 'Serial' Time: {accumulatedMsec} msec.  Ratio: {accumulatedMsec / totalProcessingTime.Elapsed.TotalMilliseconds}");

// runs some quality checks on how messages were processed.
void RunQualityCheck(bool podidxcheck = true, bool logindividual = false)
{
    for (int i = 0; i < outboundMessages.Count; i++)
    {
        MessageOut message = outboundMessages[i];

        if (i > 0)
        {
            //sanity check on our processing order
            if (message.Id - outboundMessages[i - 1].Id != 1)
            {
                _logger.Error($"{message.Id:D6}:{message.ProcessedByPodIdx:D3}:{message.RuntimeMsec:D7}");
                throw new InvalidOperationException($"{message.Id}");
            }
            if (podidxcheck)
            {
                //just warn, this is not a problem in and of itself
                var pbp = outboundMessages[i].ProcessedByPodIdx;
                var pbpp = outboundMessages[i - 1].ProcessedByPodIdx;

                var exppp = pbp == 0 ? appParams.PodCount - 1 : pbp - 1;

                if (exppp != pbpp)
                {
                    _logger.Warning($"Pod Missed.  Message Previous to {message.Id} processed by wrong pod.  expected {exppp} actual {pbpp}");
                }
            }
        }

        accumulatedMsec += message.RuntimeMsec;

        if (logindividual)
        {
            _logger.Information($"{message.Id:D6}:{message.ProcessedByPodIdx:D3}:{message.RuntimeMsec:D7}:{message.Text}");
        }
    }
}


