# SR Queue Consumer

This repo contains the source code for the project described [here](https://calhuskerfan.github.io/srqc).  Please refer to the write up for detailed information.

## Introduction

The SR Queue Consumer processes messages in parallel while mainting their FIFO order.  Messages are processed by containers (pods) responsible for message transformation on a worker thread.  Pods are kept in order by a concurrent queue.

## Quick Start

Pre-requisites
- git
- dotnet9
- VSCode or Visual Studio

Steps
- sync this repo
- from a command prompt
    ```ps1
    dotnet run --project .\src\console\ -c Release
    ```
- when the process finishes you will see a breakdown of the messages processed as well as a summary of net throughput.

## Details

The project consists of the sqrc library and a console application.

There are two primary modes that the console application will run in for demonstration.  Each is described below:

### Default mode

In the default mode you set up some parameters and a set of messages is generated with random processing times.

At the top of the program.cs file there are some default configuration settings.

```csharp
ApplicationParameters appParams = new()
{
     PodCount = 3,
    MessageCount = 13,
    MinProcessingDelay = 75,
    MaxProcessingDelay = 225,
};
```

| Parameter | Description |
| --------- | ----------- |
| Pod Count | Number of pods for message processing
| MessageCount | Number of Messages to process |
| MinProcessingDelay | The minimum simulation delay assigned to a message |
| MaxProcessingDelay | The maximum simulation delay assigned to a message |

### Scenario Mode

In this mode, a fixed set of messages is generated to test or demonstrate a specific scenarion.  This is not as polished as it should be.

1. Update line (approx) 30

    ```csharp
    //update to run specific scenarios.  see LoadInboundMessages for details.
    int testCase = 0;
    ```
1. Run the project
    ```ps1
    dotnet run --project .\src\console\ -c Release
    ```