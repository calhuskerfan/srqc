using Serilog;
using srqc;
using srqc.domain;
using System.Diagnostics;

ApplicationParameters appParams = new()
{
    PodCount = 1,
    MessageCount = 30,
    MinProcessingDelay = 100,
    MaxProcessingDelay = 200,
};

Log.Logger = new LoggerConfiguration()
  .Enrich.WithThreadId()
  .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {ThreadId,3} {Message:lj}{NewLine}{Exception}")
  .MinimumLevel.Debug()
  .CreateLogger();

ILogger _logger = Log.Logger;

_logger.Information("Starting");

var processingContainer = GetProcessingContainer(appParams);

Random r = new();

// inbound and outbound message 'queues'
List<MessageIn> inboundMessages = [];
List<MessageOut> outboundMessages = [];


processingContainer.MessageReadyAtExitEvent += (object sender, MessageReadyEventArgs e) =>
{
    outboundMessages.Add(e.Message);
};


// add messages to the inbound queue
for (int i = 1; i < appParams.MessageCount + 1; i++)
{
    inboundMessages.Add(new MessageIn()
    {
        Id = i,
        Text = i.ToString(),
        ProcessingMsec = r.Next(appParams.MinProcessingDelay, appParams.MaxProcessingDelay)
    });
}

// overall timer
Stopwatch totalProcessingTime = Stopwatch.StartNew();

_logger.Information("Loading");

// start sending messages into the queue
for (int i = 0; i < inboundMessages.Count; i++)
{
    processingContainer.WaitForStagingQueue();
    processingContainer.LoadMessage(inboundMessages[i]);
}

processingContainer.Stop();

totalProcessingTime.Stop();

_logger.Information("Empty");

// accumulate a rough estimate of what the 'serial' message processing time would have been
int accumulatedMsec = 0;

// set logindividual to true if you not want to see all the message results at the end
RunQualityCheck(logindividual: true);

_logger.Information($"total processing time: {totalProcessingTime.Elapsed.TotalMilliseconds} msec.  Accumulated 'Serial' Time: {accumulatedMsec} msec.  Ratio: {accumulatedMsec / totalProcessingTime.Elapsed.TotalMilliseconds}");
_logger.Information("Done");

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


IProcessingContainer GetProcessingContainer(ApplicationParameters parameters)
{
    return new Conduit(config: new ConduitConfig() { 
        PodCount = parameters.PodCount,
        ReUsePods = parameters.ReUsePods,
    });
}