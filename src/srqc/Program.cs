using Serilog;
using srqc;
using srqc.domain;
using System.Diagnostics;
using System.Runtime.CompilerServices;

ApplicationParameters appParams = new()
{
    PodCount = 7,
    MessageCount = 350,
    MinProcessingDelay = 100,
    MaxProcessingDelay = 200,
};

Log.Logger = new LoggerConfiguration()
  .Enrich.WithThreadId()
  .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {ThreadId,3} {Message:lj}{NewLine}{Exception}")
  .MinimumLevel.Information()
  .CreateLogger();

ILogger _logger = Log.Logger;

_logger.Information("Starting");

Random r = new();

// inbound and outbound message 'queues'
List<MessageIn> inboundMessages = [];
List<MessageOut> outboundMessages = [];

// configure and create the carousel, register event handler
Carousel carousel = new(config: new CarouselConfiguration()
{
    PodCount = appParams.PodCount,
    LogInvoke = false,
    SuppressNoisyINF = false
});

carousel.MessageReadyAtExitEvent += (object sender, MessageReadyEventArgs e) =>
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

// start sending messages into the queue
for (int i = 0; i < inboundMessages.Count; i++)
{
    if (i % 50 == 0)
    {
        //_logger.Information($"inbound heartbeat {i}");
        Console.WriteLine($"inbound heartbeat {i}");
    }

    carousel.WaitForStagingQueue();
    carousel.LoadMessage(inboundMessages[i]);
}

// wait for the carousel to empty
// consider making this an event
while (!carousel.IsCarouselEmpty())
{
    Thread.Sleep(1 * 500);
}

totalProcessingTime.Stop();

_logger.Information("empty");

carousel.Stop();

// accumulate a rough estimate of what the 'serial' message processing time would have been
int accumulatedMsec = 0;

// set logindividual to false if you do not want to see all the message results at the end
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
                _logger.Error($"{message.Id:D6}:{message.ProcessedByPod:D3}:{message.RuntimeMsec:D7}");
                throw new InvalidOperationException($"{message.Id}");
            }
            if (podidxcheck)
            {
                //just warn, this is not a problem in and of itself
                var pbp = outboundMessages[i].ProcessedByPod;
                var pbpp = outboundMessages[i - 1].ProcessedByPod;
                var exppp = pbp == appParams.PodCount - 1 ? 0 : pbp + 1;
                if (exppp != pbpp)
                {
                    _logger.Warning($"Pod Missed.  Message Previous to {message.Id} processed by wrong pod.  expected {exppp} actual {pbpp}");
                }
            }
        }

        accumulatedMsec += message.RuntimeMsec;

        if (logindividual)
        {
            _logger.Information($"{message.Id:D6}:{message.ProcessedByPod:D3}:{message.RuntimeMsec:D7}:{message.Text}");
        }
    }
}
