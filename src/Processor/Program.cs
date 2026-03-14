using Serilog;
using Processor;
using Srqc;
using Srqc.Domain;
using Processor.Transformers;
using HostingExtensions;

var builder = HostingExtensions.HostingExtensions.GetBuilder(args);

builder.Services
    .Configure<ConduitConfig>(builder.Configuration.GetSection("ConduitConfig"));

builder.Services
    .AddTransient<IProcessingSystem<MessageIn, MessageOut>, Conduit<MessageIn, MessageOut>>()
    .AddTransient<IWorkerContext, WorkerContext>()
    .AddSingleton<ITransformerFactory<MessageIn, MessageOut>, DefaultTransformerFactory>()
    .AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();
