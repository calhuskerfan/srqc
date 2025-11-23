using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Srqc;
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

            //move this to DI
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


            for (int i = 0; i < Convert.ToInt32(_configuration["AppSettings:MessageCount"]); i++)
            {
                MessageIn mi = new() { Id = i, Text= $"{i}" };

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mi));

                await sendChannel.BasicPublishAsync(
                    string.Empty,
                    _channelWriter.ChannelName,
                    body);
            }

            _logger.LogInformation("Finished producing {messageCount} messages", _configuration["AppSettings:MessageCount"]);
        }
    }
#pragma warning restore CS8602
}
