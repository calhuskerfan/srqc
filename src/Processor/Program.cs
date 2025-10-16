using Serilog;
using Processor;
using Srqc;

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
    .AddTransient<IProcessingSystem, Conduit>()
    .AddTransient<IWorkerContext, WorkerContext>()
    .AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();
