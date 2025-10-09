using console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using srqc;
using srqc.domain;
using System.Diagnostics;

Log.Logger = new LoggerConfiguration()
  .Enrich.WithThreadId()
  .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {ThreadId,3} {Message:lj}{NewLine}{Exception}")
  .MinimumLevel.Debug()
  .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);


Serilog.ILogger _logger = Log.Logger;


builder.Services.AddLogging(loggingBuilder => {
    loggingBuilder.ClearProviders();
    loggingBuilder.AddSerilog(dispose: true);
});

builder.Services.AddSingleton<IApplication, Application>();

builder.Services.AddTransient<IConduitConfig>(sp => {
    return new ConduitConfig()
    {
        PodCount = 3,
        ReUsePods = true,
    };
});


var host = builder.Build();



/*
IConfiguration configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
.AddJsonFile("appsettings.json", optional: false)
.Build();

var serviceProvider = new ServiceCollection()
    .AddLogging(options =>
    {
        options.ClearProviders();
        options.AddSerilog(dispose: true);
    })
    .AddTransient<IConduitConfig, ConduitConfig>()
    .AddTransient<IConfiguration>(sp => {
        return new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
    })
    .AddTransient<ILoggerFactory>(sp => {

        return LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
    }

    )
    .AddSingleton<IApplication, Application>()
    .BuildServiceProvider();
*/

_logger.Information("Starting");


// overall timer


IApplication app = host.Services.GetRequiredService<IApplication>();
app.Go();







