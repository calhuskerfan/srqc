using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Srqc;
using Srqc.MessageChannel;


namespace Consumer
{
    public interface IApplication
    {
        Task RunAsync();
    }

#pragma warning disable CS8600
#pragma warning disable CS8602

    public class Application : IApplication
    {
        private readonly ILogger<Application> _logger;
        private readonly IConfiguration _configuration;

        //turn these into configurations
        private readonly ChannelReader _channelReader;

        public Application(ILogger<Application> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            _channelReader = new ChannelReader() { 
                ChannelName = _configuration["AppSettings:OutQueue"].ToString() 
            };
        }

        public async Task RunAsync()
        {
            int lastId = -1;

            _logger.LogInformation("Application - RunAsync");

            var exitEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: _channelReader.ChannelName, 
                durable: true, 
                exclusive: false, 
                autoDelete: false, 
                arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += (model, ea) =>
            {
                MessageOut messgeOut = JsonConvert.DeserializeObject<MessageOut>(Encoding.UTF8.GetString(ea.Body.ToArray()));

                if (messgeOut.MessageInId != 0 
                    && messgeOut.MessageInId != lastId + 1)
                {
                    _logger.LogError("Oh shit");
                }

                lastId  = messgeOut.MessageInId;

                _logger.LogDebug("Message Id: {messageId}", messgeOut.MessageInId);

                return Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(
                _configuration["AppSettings:OutQueue"].ToString(), 
                autoAck: true,
                consumer: consumer);

            _logger.LogInformation("Connected Ctrl+C to exit");

            exitEvent.WaitOne();

            return;
        }
    }
#pragma warning restore CS8602
#pragma warning restore CS8600
}
