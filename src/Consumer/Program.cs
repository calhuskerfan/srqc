using Consumer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build())
  .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog(dispose: true);
    });

builder.Services
    .AddSingleton<IApplication, Application>();

await builder.Build()
    .Services
    .GetRequiredService<IApplication>()
    .RunAsync();