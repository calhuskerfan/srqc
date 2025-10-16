# SRQC Utilizing RabbitMQ

This section describes running the SRQC in a hosted service processing messages managed by an inbound and outboud [RabbitMQ](https://www.rabbitmq.com/) queue.

## Introduction

Environment
- git
- dotnet9
- VSCode or Visual Studio
- docker desktop [Windows Installer](https://docs.docker.com/desktop/setup/install/windows-install/)
- [Windows Terminal](https://learn.microsoft.com/en-us/windows/terminal/install)

The system is comprised of the following four elements.

1. RabbitMQ running as a docker container
1. Console application to `produce` messages
1. IHostedService application to `process` messages
1. IHostedService application to `consume` messages

Both Processer and Consumer are [IHostedService](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice?view=net-9.0-pp) implemented as [BackgroundServiceClass](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=net-9.0-pp) 

## Quickstart

Open a powershell terminal and navigate to:
./scripts

``` ps1
# pull the rabbitmq docker image
docker pull rabbitmq:4-management
# start rabbit, and the applications.
.\start-all.ps1
```
`start-all.ps1` will:
1. start the rabbitmq container
1. launch a browser pointed to localhost:http://localhost:15672/
1. start the processing hosted service in a terminal window
1. start the consuming holsted service in a terminal window
1. start the producing application in a terminal window.

The producing application will shut down once it has sent its prescribed message count.  The processing and consuming services will remain running until the user closes them down.

## Details and Other Execution options

### RabbitMQ running as a docker container

```ps
docker run `
    --name rabbitmq-srqc `
    -p 5672:5672 `
    -p 15672:15672 `
    -d `
    rabbitmq:4-management
```

[rabbit mq](http://localhost:15672/).  login should be guest/guest

### IHostedService application to process messages

Open A New Terminal Window (or Tab)

```ps1
  # from the ./src/Service directory
  dotnet run -c Release
```
### IHostedService application to Consume messages

Open A New Terminal Window (or Tab)

```ps1
  # from the ./src/Consumer directory
  dotnet run -c Release
```

### Console application to Produce messages

Open A New Terminal Window (or Tab)

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