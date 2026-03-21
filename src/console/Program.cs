using Console;
using Console.Transformers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Srqc;
using Srqc.Domain;

var builder = HostingExtensions.HostingExtensions.GetBuilder(args);

// set up all our services
builder.Services
    .Configure<ConduitConfig>(builder.Configuration.GetSection("ConduitConfig"))
    .AddTransient<IProcessingSystem<MessageIn, MessageOut>, Conduit<MessageIn, MessageOut>>()
    .AddSingleton<IApplication, Application>()
    .AddSingleton(
        typeof(ITransformerFactory<MessageIn, MessageOut>), 
        GetTransformerFactory(builder.Services.BuildServiceProvider()));

builder.Build()
    .Services
    .GetRequiredService<IApplication>()
    .Run();


/// <summary>
/// GetTransformerFactory.  This is not the greatest way to do this,
/// but it helps demonstrate how
/// the transformer is configured outside of the queue processing.
/// </summary>
static Type GetTransformerFactory(IServiceProvider serviceProvider)
{
    IConfiguration? configuration = serviceProvider?.GetService<IConfiguration>();

    if (configuration == null)
    {
        return typeof(DefaultTransformerFactory);
    }

    var type = configuration["ConduitConfig:TransformerFactoryType"];

    if (type == null)
    {
        return typeof(DefaultTransformerFactory);
    }

#pragma warning disable CS8603
    return Type.GetType(type);
#pragma warning restore CS8603
}
