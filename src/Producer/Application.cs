using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Srqc.Domain;
using Srqc.MessageChannel;
using System.Text;


namespace Producer
{
    public interface IApplication
    {
       Task RunAsync();
    }

#pragma warning disable CS8602

    public class Application : IApplication
    {
        private readonly ILogger<Application> _logger;
        private readonly IConfiguration _configuration;
        private readonly ChannelWriter _channelWriter;

        public Application(ILogger<Application> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            _channelWriter = new ChannelWriter()
            {
                ChannelName = _configuration["OutboundChannel:Name"].ToString()
            };
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("Run");

            var factory = new ConnectionFactory { HostName = "localhost" };
            using var sendConnection = await factory.CreateConnectionAsync();
            using var sendChannel = await sendConnection.CreateChannelAsync();

            await sendChannel.QueueDeclareAsync(
                queue: _channelWriter.ChannelName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            int cycle = 0;
            int messagesPerCycle = Convert.ToInt32(_configuration["AppSettings:MessagesPerCycle"]);
            int total = 0;

            while (true)
            {
                for (int i = cycle * messagesPerCycle; i < messagesPerCycle + (cycle * messagesPerCycle); i++)
                {
                    MessageIn mi = new() { Id = total, Text = $"{total}" };

                    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mi));

                    await sendChannel.BasicPublishAsync(
                        string.Empty,
                        _channelWriter.ChannelName,
                        body);

                    total++;
                }

                _logger.LogInformation("Finished producing {messageCount} messages", messagesPerCycle);

                Console.WriteLine("A to run another message cycle.  Any other key exists");
                var nexta = Console.ReadKey();
                
                if(!(nexta.Key == ConsoleKey.A))
                {
                    break;
                }
            }
        }
    }
#pragma warning restore CS8602
}
