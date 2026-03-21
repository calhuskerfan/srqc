# SQRC Console Application

There are two primary modes that the console application will run in for demonstration.  Each is described below:

## Default mode

In the default mode a series of messages is generated from parameters defined in the appsettings.json file.

```ps1
dotnet run -c Release
```

In the appsettings.json file

```json
{
  "AppSettings": {
    "TestScenario": 0,
    "MinProcessingDelay": 100,
    "MaxProcessingDelay": 300,
    "MessageCount": 13,
    "TransformerFactoryType": "Console.Transformers.DefaultTransformerFactory"

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

## Scenario Mode

In this mode, a fixed set of messages is generated to test or demonstrate a specific scenario.  The command arguments assume no other adjustments to the appsettings.json file.

Test Scenario 1 creates 4 messages where the third message takes the longest to process.  While the third message is processing the fourth is picked up and completed.  When the third is completed the fourth is also available.

```ps1
# from the ./src/console directory
dotnet run -c Release -- --AppSettings:TestScenario 1 --ConduitConfig:PodCount 3
```

Test Scenario 2 creates 5 messages.  Pods 0 and 1 will each complete 2 messages while pod 2 completes one.

```ps1
# from the ./src/console directory
dotnet run -c Release -- --AppSettings:TestScenario 2 --ConduitConfig:PodCount 3
```

Finally as a means of demonstrating the decoupling of the message processing from the message queue system ConduitConfig:TransformerFactoryType can be set to change the message transformation.

The Two Options are:

| TransformerFactoryType | Description |
| ---------------------- | ----------- |
| Console.Transformers.DefaultTransformerFactory | Default Transformer.  Maps input message text property to output message text property |
| Console.Transformers.LoggingTransformerFactory | Injects a logger and logs tracing for GetTransformer() and Transform() |

```ps1

dotnet run -c Release `
  --ConduitConfig:TransformerFactoryType "Console.Transformers.LoggingTransformerFactory" `
  --Serilog:MinimumLevel:Default "Verbose"

```

Running with --ConduitConfig:ReUsePods will generate more calls to the Factory Method.

```ps1
dotnet run -c Release `
  --ConduitConfig:TransformerFactoryType "Console.Transformers.LoggingTransformerFactory" `
  --ConduitConfig:ReUsePods false `
  --Serilog:MinimumLevel:Default "Verbose"
```
