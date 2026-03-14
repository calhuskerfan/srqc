using Console;
using Console.Transformers;
using Microsoft.Extensions.DependencyInjection;
using Srqc;
using Srqc.Domain;

var builder = HostingExtensions.HostingExtensions.GetBuilder(args);

builder.Services
    .Configure<ConduitConfig>(builder.Configuration.GetSection("ConduitConfig"))
    .AddTransient<IProcessingSystem<MessageIn, MessageOut>, Conduit<MessageIn, MessageOut>>()
    .AddSingleton<ITransformerFactory<MessageIn, MessageOut>, LoggingTransformerFactory>()
    .AddSingleton<IApplication, Application>();

builder.Build()
    .Services
    .GetRequiredService<IApplication>()
    .Run();
