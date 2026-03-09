using Serilog;
using Processor;
using Srqc;
using Srqc.Domain;

Log.Logger = new LoggerConfiguration()
  .Enrich.WithThreadId()
  .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {ThreadId,3} {Message:lj}{NewLine}{Exception}")
  .MinimumLevel.Information()
  .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog(dispose: true);
    })
    .Configure<ConduitConfig>(builder.Configuration.GetSection("ConduitConfig"));

builder.Services
    .AddTransient<IProcessingSystem<MessageIn, MessageOut>, Conduit<MessageIn, MessageOut>>()
    .AddTransient<IWorkerContext, WorkerContext>()
    .AddSingleton<ITransformerFactory<MessageIn, MessageOut>, Transformer>()
    .AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();
