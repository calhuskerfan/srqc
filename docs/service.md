# SRQC Utilizing RabbitMQ

This section describes running the SRQC in an [IHostedService](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice?view=net-10.0-pp) processing messages managed by an inbound and outboud [RabbitMQ](https://www.rabbitmq.com/) queue.

## Introduction

Environment

- dotnet10
- git
- VSCode or Visual Studio
- docker desktop. [Windows Installer](https://docs.docker.com/desktop/setup/install/windows-install/)
- [Windows Terminal](https://learn.microsoft.com/en-us/windows/terminal/install)

The system is comprised of the following four elements.

1. RabbitMQ running as a docker container
1. Console application to `produce` messages
1. IHostedService application to `process` messages
1. IHostedService application to `consume` messages

Both Processer and Consumer are [IHostedService](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice?view=net-10.0-pp) implemented as [BackgroundService](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=net-10.0-pp) class.

## Quickstart

Open a powershell terminal and navigate to:
./scripts

``` ps1
# run the following to pull rabbitmq docker image
docker pull rabbitmq:4-management
```

``` ps1
# start rabbit
.\start-rabbit.ps1
```

1. Start the RabbitMQ image in a container.
1. Launch a browser pointed to the RabbitMQ manager localhost at http://localhost:15672/.  The login is guest/guest.

The script includes a 20 second delay since the demonstration applications do not have connection retries.  This is on the list of updates.

``` ps1
# start rabbit
.\start-applications.ps1
```

`start-applications.ps1` will:

1. Start the processing hosted service in a terminal window.
1. Start the consuming hosted service in a terminal window.
1. Start the producing application in a terminal window.

The producing application will send the configured number of messages as determied by the `AppSettings::MessagesPerCycle` to the inbound queue.  You can send more messages or exit from the window.

The processing and consuming services will remain running until the user closes them.

## Details and Other Execution options

The demonstration system is made of four primary components, each is described below.

### RabbitMQ running as a docker container

The RabbitMQ container serves as the message broker to connect the producer to the processor and the processor to the consumer.

```ps
docker run `
    --name rabbitmq-srqc `
    -p 5672:5672 `
    -p 15672:15672 `
    -d `
    rabbitmq:4-management
```

http://localhost:15672/.  login should be guest/guest

### IHostedService application to Process messages

Process messages from the inbound queue and delivers them to the outbound queue.

```ps1
  # from the ./src/Processor directory
  dotnet run -c Release
```

The processor contains two TransformerFactoryType.

| TransformerFactoryType | Description |
| ---------------------- | ----------- |
| Console.Transformers.DefaultTransformerFactory | Default Transformer.  Maps input message text property to output message text property |
| Console.Transformers.ExternalServiceTransformerFactory | Calls an external url as part of the processing |

```ps1
  # from the ./src/Processor directory

dotnet run -c Release `
  --ConduitConfig:TransformerFactoryType "Processor.Transformers.ExternalServiceTransformerFactory" `
  --Serilog:MinimumLevel:Default "Verbose"
```

### IHostedService application to Consume messages

Consumes messages produced by the processor.

```ps1
  # from the ./src/Consumer directory
  dotnet run -c Release
```

### Console application to Produce messages

Produces message for the processor to transform.

```ps1
  # from the ./src/Producer directory
  dotnet run -c Release
```

To pass in a different message count:

```ps1
  # from the ./src/Producer directory
  dotnet run -c Release -- --AppSettings:MessageCount 172
```

### Stop and Remove RabbitMQ

```ps1
docker rm -f rabbitmq-srqc
```
