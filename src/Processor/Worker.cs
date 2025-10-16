using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;
using Srqc;

namespace Processor
{
#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8622

    public class Worker : BackgroundService
    {
        internal static Random r = new();

        private readonly ILogger<Worker> _logger;
        private readonly IProcessingSystem _processingSystem;
        private readonly IConfiguration _configuration;
        private readonly IWorkerContext _context;

        private readonly string _outQueueName;
        private readonly string _outQueueRoutingKey;
        private readonly string _inQueueName;

        //turn these into configurations
        private readonly ushort _prefetchCount = 3;

        public Worker(
            ILogger<Worker> logger,
            IWorkerContext context,
            IProcessingSystem processingSystem,
            IConfiguration configuration)
        {
            _logger = logger;
            _processingSystem = processingSystem;
            _configuration = configuration;
            _context = context;
            _inQueueName = _configuration["AppSettings:InQueue"].ToString();
            _outQueueName = _configuration["AppSettings:OutQueue"].ToString();
            _outQueueRoutingKey = _configuration["AppSettings:OutQueue"].ToString();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExecuteAsync");

            var factory = new ConnectionFactory { HostName = "localhost" };

            using var sendConnection = await factory.CreateConnectionAsync();
            using var sendChannel = await sendConnection.CreateChannelAsync();

            await sendChannel.QueueDeclareAsync(
                queue: _outQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _processingSystem.MessageReadyAtExitEvent += (object sender, MessageReadyEventArgs e) =>
            {
                _logger.LogInformation(e.Message.ToString());

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(e.Message));

                sendChannel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: _outQueueRoutingKey,
                    body: body)
                .GetAwaiter()
                .GetResult();
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: _inQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await channel.BasicQosAsync(0, _prefetchCount, false);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += (model, ea) =>
            {
                IClaimCheck claimCheck = _processingSystem.WaitForProcessingSlotAvailable();

                MessageIn mi = JsonConvert.DeserializeObject<MessageIn>(Encoding.UTF8.GetString(ea.Body.ToArray()));
                mi.ProcessingMsec = r.Next(_context.MinProcessingDelay, _context.MaxProcessingDelay);

                _processingSystem.LoadMessage(claimCheck, mi);

                _ = channel.BasicAckAsync(ea.DeliveryTag, false);

                return Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(
                _inQueueName,
                autoAck: false,
                consumer: consumer);

            // sit and wait
            _logger.LogInformation("Waiting for Shutdown Event");
            stoppingToken.WaitHandle.WaitOne();

            // tell system to stop processing
            _logger.LogInformation("Stop Processing System");
            _processingSystem.Stop();

            // we are all done
            _logger.LogInformation("Done");
        }
    }
#pragma warning restore CS8622
#pragma warning restore CS8602
#pragma warning restore CS8600
}
