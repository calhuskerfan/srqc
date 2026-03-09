using Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Srqc;
using Srqc.Domain;

Log.Logger = new LoggerConfiguration()
  .Enrich.WithThreadId()
  .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {ThreadId,3} {Message:lj}{NewLine}{Exception}")
  .MinimumLevel.Information()
  .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

//TODOCJH:  Rewview Using UseSerilog(...) instead of add logging.

builder.Services
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog(dispose: true);
    })
    .Configure<ConduitConfig>(builder.Configuration.GetSection("ConduitConfig"))
    .AddTransient<IProcessingSystem<MessageIn, MessageOut>, Conduit<MessageIn, MessageOut>>()
    .AddSingleton<ITransformerFactory<MessageIn, MessageOut>, Transformer>()
    .AddSingleton<IApplication, Application>();

builder.Build()
    .Services
    .GetRequiredService<IApplication>()
    .Run();







