using Processor;
using Srqc;
using Srqc.Domain;
using Processor.Transformers;

var builder = HostingExtensions.HostingExtensions.GetBuilder(args);

builder.Services
    .Configure<ConduitConfig>(builder.Configuration.GetSection("ConduitConfig"));

builder.Services
    .AddTransient<IProcessingSystem<MessageIn, MessageOut>, Conduit<MessageIn, MessageOut>>()
    .AddTransient<IWorkerContext, WorkerContext>()
    .AddSingleton(
        typeof(ITransformerFactory<MessageIn, MessageOut>),
        GetTransformerFactory(builder.Services.BuildServiceProvider()))
    .AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();



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
