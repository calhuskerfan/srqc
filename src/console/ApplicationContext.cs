using Serilog;
using Srqc;

namespace console
{
    public class ApplicationContext
    {
        private readonly ILogger _logger = Log.ForContext<ApplicationContext>();

        // inbound and outbound message 'queues'
        public List<MessageIn> inboundMessages = [];
        public List<MessageOut> outboundMessages = [];
        public ApplicationParameters appParams = new();

        public int AccumulatedMsec { get; set; } = 0;

        public void RunQualityCheck(int podCount, bool podidxcheck = true, bool logindividual = false)
        {
            AccumulatedMsec = 0;

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

                        var exppp = pbp == 0 ? podCount - 1 : pbp - 1;

                        if (exppp != pbpp)
                        {
                            _logger.Warning($"Pod Missed.  Message Previous to {message.Id} processed by wrong pod.  expected {exppp} actual {pbpp}");
                        }
                    }
                }

                AccumulatedMsec += message.RuntimeMsec;

                if (logindividual)
                {
                    _logger.Information($"{message.Id:D6}:{message.ProcessedByPodIdx:D3}:{message.RuntimeMsec:D7}:{message.Text}");
                }
            }
        }
    }
}
