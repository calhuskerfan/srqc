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

builder.Services
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog(dispose: true);
    })
    .Configure<ConduitConfig>(builder.Configuration.GetSection("ConduitConfig"))
    .AddSingleton<IApplication, Application>();

//package it all up and go

builder.Build()
    .Services
    .GetRequiredService<IApplication>()
    .Run();







