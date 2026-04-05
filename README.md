# Smugglers Run Queue Consumer

This repo contains the source code for the project described [here](https://calhuskerfan.github.io/2026/03/05/SQRC-part3.html).  Please refer to the write up for detailed information.

## Introduction

The Smugglers Run Queue Consumer (SRQC) processes messages in parallel while maintaining First In First Out (FIFO) order.  Messages are processed by containers (pods) responsible for message transformation on a worker thread.  Pods are kept in order by a dotnet concurrent queue.

Additionally the Message Types, Inbound and Oubound, as well as the message transformation logic are declared at runtime allowing for the SRQC to be a re-usable library.

## Quick Start

Pre-requisites

- git
- dotnet10
- VSCode or Visual Studio
- [Windows Terminal](https://learn.microsoft.com/en-us/windows/terminal/install)

Steps

- sync this repo
- from a command prompt navigate to the .\src\console\ directory
- run

    ```ps1
    # from the ./src/console directory
    dotnet run -c Release
    ```

- when the process completes a breakdown of the messages processed is displayed in addition to a throughput summary.

## Details

The project consists of the SRQC library and two consumption demonstrations.  The first is a self contained console application, and the second is running inside a Hosted service consuming and delivering messages via a message broker service.
 - refer to [console details](./docs/console.md) for console mode operations and options
 - refer to [service instructions](./docs/service.md) for details on running service, producer, and consumer.

