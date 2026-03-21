using Microsoft.Extensions.Logging;
using Srqc;
using Srqc.Domain;

namespace Console.Transformers
{
    public class LoggingTransformerFactory : ITransformerFactory<MessageIn, MessageOut>
    {
        private readonly ILogger<LoggingTransformerFactory> _logger;

        public LoggingTransformerFactory(ILogger<LoggingTransformerFactory> logger)
        {
           _logger = logger;
        }

        public Func<MessageIn, MessageOut> GetTransformer()
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Getting a new transformer function from the TransformerFactory to process MessageIn and produce MessageOut.");
            }

            return (MessageIn m) =>
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Func<MessageIn, MessageOut> Processing MessageIn with Id: {MessageId} and Text: {MessageText}", m.Id, m.Text);
                }

                Thread.Sleep(m.ProcessingMsec);
                
                return new MessageOut
                {
                    Text = $"New outbound message is: {m.Text}",
                    Id = m.Id + 10000,
                    MessageInId = m.Id,
                };
            };
        }
    }
}
