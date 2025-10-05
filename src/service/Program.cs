using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using service;
using srqc.domain;

Log.Logger = new LoggerConfiguration()
  .Enrich.WithThreadId()
  .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {ThreadId,3} {Message:lj}{NewLine}{Exception}")
  .MinimumLevel.Debug()
  .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(loggingBuilder => {
    loggingBuilder.ClearProviders();
    loggingBuilder.AddSerilog(dispose: true);
});

builder.Services.AddHostedService<Worker>();

builder.Services.AddTransient<IConduitConfig>(sp => { return new ConduitConfig()
{
    PodCount = 3,
    ReUsePods = true,
}; });


var host = builder.Build();

host.Run();
