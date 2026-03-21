using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;


namespace HostingExtensions
{
    public static class HostingExtensions
    {
        public static HostApplicationBuilder GetBuilder(string[]? args)
        {
            var builder = Host.CreateApplicationBuilder(args);

#pragma warning disable CS8604 // Possible null reference argument.
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .Build())
              .CreateLogger();
#pragma warning restore CS8604 // Possible null reference argument.

            builder.Services
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog(dispose: true);
                });

            return builder;
        }
    }
}
