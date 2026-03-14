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

            return builder;
        }
    }
}
