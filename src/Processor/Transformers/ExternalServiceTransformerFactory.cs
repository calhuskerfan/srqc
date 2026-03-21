using Srqc;
using Srqc.Domain;

namespace Processor.Transformers
{
    public class ExternalServiceTransformerFactory : ITransformerFactory<MessageIn, MessageOut>
    {

        private readonly ILogger<ExternalServiceTransformerFactory> _logger;
        private readonly IConfiguration _configuration;

        public ExternalServiceTransformerFactory(
            ILogger<ExternalServiceTransformerFactory> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
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

                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        HttpResponseMessage response = client.GetAsync("http://example.com").Result;
                        response.EnsureSuccessStatusCode();

                        string responseBody = response.Content.ReadAsStringAsync().Result;

                        _logger.LogInformation("Successfully called external service for MessageIn with Id: {MessageId}.  {ResponseBody}", m.Id, responseBody);

                        // Return the result
                        //return responseBody;
                    }
                    catch (HttpRequestException e)
                    {
                        _logger.LogError(e, "HTTP request failed while processing MessageIn with Id: {MessageId}", m.Id);
                    }
                }




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
