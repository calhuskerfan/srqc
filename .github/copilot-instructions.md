# Copilot Instructions for SRQC

## Build and Run Commands

**Build the solution:**
```bash
dotnet build
```

**Run the console application (default mode with appsettings.json config):**
```bash
# from ./src/Console directory
dotnet run -c Release
```

**Run console with test scenario:**
```bash
# Scenario 1: Third message takes longest, fourth completes while waiting
dotnet run -c Release -- --AppSettings:TestScenario 1 --ConduitConfig:PodCount 3

# Scenario 2: Load balancing demo (pods complete different message counts)
dotnet run -c Release -- --AppSettings:TestScenario 2 --ConduitConfig:PodCount 3
```

**Run with different transformer (includes logging):**
```bash
dotnet run -c Release `
  --ConduitConfig:TransformerFactoryType "Console.Transformers.LoggingTransformerFactory" `
  --Serilog:MinimumLevel:Default "Verbose"
```

**Run service-based demo (RabbitMQ):**
```bash
# Terminal 1: Start RabbitMQ container
docker pull rabbitmq:4-management
docker run --name rabbitmq-srqc -p 5672:5672 -p 15672:15672 -d rabbitmq:4-management

# Terminal 2: Run Processor (./src/Processor)
dotnet run -c Release

# Terminal 3: Run Consumer (./src/Consumer)
dotnet run -c Release

# Terminal 4: Run Producer (./src/Producer)
dotnet run -c Release -- --AppSettings:MessageCount <number>
```

**Clean up RabbitMQ:**
```bash
docker rm -f rabbitmq-srqc
```

## Architecture Overview

SRQC (Smugglers Run Queue Consumer) is a parallel message processing system that maintains FIFO order while processing messages concurrently.

### Core Components

1. **Conduit** (`src/Srqc/Conduit.cs`) - Main processing system that orchestrates pods and manages message flow
   - Uses `ConcurrentQueue<Pod>` to maintain message order
   - Manages pod lifecycle and availability
   - Emits `MessageReadyAtExitEvent` when messages complete

2. **Pod** (`src/Srqc/Pod.cs`) - Processing container (like Kubernetes Pod) handling individual messages
   - State machine: `NotInitialized → WaitingToLoad → Loading → Running → Completed → ReadyToUnload → Unloading`
   - Thread-safe state management via `volatile` and `Interlocked`
   - Tracks execution time and message processing

3. **MessageChannel** (`src/Srqc/MessageChannel/`) - Channel abstraction for message handling
   - `ChannelBase` - Base implementation
   - `ChannelReader` - Reads from channel
   - `ChannelWriter` - Writes to channel

4. **Transformer Pattern** - Generic message transformation
   - `ITransformer<TIn, TOut>` - Transform a single message
   - `ITransformerFactory<TIn, TOut>` - Factory producing transformation functions
   - Runtime-configurable via `ConduitConfig:TransformerFactoryType`

### Projects

- **Srqc** - Core library containing Conduit and Pod implementations
- **Domain** - Shared domain models (MessageIn, MessageOut)
- **Console** - Standalone demo with configurable scenarios and transformers
- **Processor** - IHostedService that consumes from RabbitMQ inbound queue
- **Consumer** - IHostedService that consumes from RabbitMQ outbound queue
- **Producer** - Console app that generates messages to RabbitMQ
- **HostingExtensions** - DI and hosting configuration utilities

## Key Conventions

### Generic Type Parameters
All processing is generic: `TMessageIn` and `TMessageOut` allow different input/output types. This enables runtime configuration of message types and transformation logic.

### Configuration via appsettings.json
- **ConduitConfig** - Pod count, pod reuse strategy, transformer factory type
- **AppSettings** - Test scenario selection, message count, processing delays
- **Serilog** - Logging configuration

### Transformer Factory Pattern
Transformers are obtained via factory methods to allow switching implementations at runtime without recompiling:
```csharp
// In Program.cs
.AddSingleton(
    typeof(ITransformerFactory<MessageIn, MessageOut>), 
    GetTransformerFactory(builder.Services.BuildServiceProvider()));
```

Console supports two factories:
- `Console.Transformers.DefaultTransformerFactory` - Simple text property mapping
- `Console.Transformers.LoggingTransformerFactory` - Injects logging for diagnostics

Processor supports two factories:
- `Processor.Transformers.DefaultTransformerFactory`
- `Processor.Transformers.ExternalServiceTransformerFactory` - Calls external HTTP service

### Pod Reuse Strategy
When `ConduitConfig:ReUsePods` is true, pods are reused for multiple messages. When false, new pods are created for each message. This affects factory invocation frequency.

### State Management
Pod state uses volatile fields and `Interlocked` operations for thread-safe updates without locks, optimizing for high-concurrency scenarios:
```csharp
private volatile int _podstate = (int)PodState.NotInitialized;
public PodState State
{
    get { return (PodState)_podstate; }
    set { Interlocked.Exchange(ref _podstate, (int)value); }
}
```

### Threading Model
- Main orchestration in Conduit via `Thread _unloadProcessingThread`
- Each Pod processes messages on its own worker thread
- `EventWaitHandle` used for synchronization (WaitToLoadHandle, ShutdownCompleteHandle)
- ConcurrentQueue for thread-safe pod management

### Interfaces
The library defines clear contracts to enable extensibility:
- `IProcessingSystem<TIn, TOut>` - Core processing interface
- `IProcessingContainer<TIn, TOut>` - Pod interface
- `ITransformer<TIn, TOut>` & `ITransformerFactory<TIn, TOut>` - Transformation contracts
- `IClaimCheck` - Ticket for message slot reservation
- `IConduitConfig` - Configuration interface

## Important Notes

- The system is designed for high throughput with FIFO guarantees
- Pods maintain order via ConcurrentQueue while processing happens in parallel
- Configuration is decoupled from compilation: transformer types are configurable via appsettings
- Event-driven completion: consume `MessageReadyAtExitEvent` for processed messages
- Net10.0 target framework; C# nullable reference types enabled
