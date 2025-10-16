# SR Queue Consumer

This repo contains the source code for the project described [here](https://calhuskerfan.github.io/2025/09/28/SQRC.html) and additional components described [here]().  Please refer to the write up for detailed information.

## Introduction

The SR Queue Consumer processes messages in parallel while maintaining their order.  Messages are processed by containers (pods) responsible for message transformation on a worker thread.  Pods are kept in order by a concurrent queue.

## Quick Start

Pre-requisites
- git
- dotnet9
- VSCode or Visual Studio

Steps
- sync this repo
- from a command prompt navigate to the .\src\console\ directory
- from a command run
    ```ps1
    # from the ./src/console directory
    dotnet run -c Release
    ```
- when the process completes a breakdown of the messages processed is displayed in addition to a throughput summary.

## Details

The project consists of the srqc library and two consumption demonstrations.  The first is a self contained console application, and the second is running inside a Hosted service consuming and delivering messages contained in a message queing service.  This document covers the console application operations, refer here for [instructions](./docs/service.md) on running service, producer, and consumer.

There are two primary modes that the console application will run in for demonstration.  Each is described below:

### Default mode

In the default mode a series of messages is generated from parameters defined in the appsettings.json file with random processing times.

In the appsettings.json file

```json
{
  "AppSettings": {
    "TestScenario": 0,
    "MinProcessingDelay": 100,
    "MaxProcessingDelay": 300,
    "MessageCount": 13
  },
  "ConduitConfig": {
    "PodCount": 3,
    "ReUsePods": true
  }
}
```

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| Pod Count | int | Number of pods for message processing
| MinProcessingDelay | int | The minimum simulation delay assigned to a message |
| MaxProcessingDelay | int | The maximum simulation delay assigned to a message |
| TestScenario | int | Integer value of Test Scenario to run [0,1,2] |
| ReUsePods | boolean | true to reuse pod instances, false to create a new pod instance for each message |

### Scenario Mode

In this mode, a fixed set of messages is generated to test or demonstrate a specific scenario.  The command arguments assume no other adjustments to the appsettings.json file.

Test Scenario 1 creates 4 messages where the third message takes the longest to process.  While the third message is processing the fourth is picked up and completed.  When the third is completed the fourth is also available.

```ps1
# from the ./src/console directory
dotnet run -c Release -- --AppSettings:TestScenario 1 --ConduitConfig:PodCount 3
```

Test Scenario 2 creates 5 messages.  Pods 0 and 1 will each complete 2 messages while pod 3 completes one.

```ps1
# from the ./src/console directory
dotnet run -c Release -- --AppSettings:TestScenario 2 --ConduitConfig:PodCount 3
```
