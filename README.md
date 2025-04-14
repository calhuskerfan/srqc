# SR Queue Consumer

This project is the source code for the project described here [srqc](https://calhuskerfan.github.io/srqc)

## Introduction

Hopefully the comments in the code are useful to navigate and understand what is going in in case you want to experiment for yourself.

## Running

Pre-requisites
- git
- dotnet9
- VSCode or Visual Studio

Steps
- sync this repo
- from a command prompt
    ```ps1
    dotnet run --project src/srqc -c Release
    ```
            
At the top of the program.cs file there are some default configuration settings.

```csharp
ApplicationParameters appParams = new()
{
    PodCount = 7,
    MessageCount = 350,
    MinProcessingDelay = 100,
    MaxProcessingDelay = 200,
};
```

## Some Random Notes

1.  At very low delays I am seeing some thread deadlocking issues, I think it is an issue with the rotate lock in the waiting for pod to unload callback.  The deadlock appears to be in the event handler for OnMessageReady.
    ```csharp
        protected virtual void OnMessageReady(MessageReadyEventArgs e)
        {
            if (_config.LogInvoke)
            {
                _logger.Information("Pod {idx}: Invoke Message Ready {messageId}", e.Message.ProcessedByPod, e.Message.Id);
            }

            MessageReadyAtExitEvent?.Invoke(this, e);
        }
    ```
    To repeat:
    1. Set the MinProcessingDelay to 0 and the MaxProcessingDelay to 50.
    1. Set the `LogInvoke` property on the CarouselConfiguration to `false`, and the `SuppressNoisyINF` to `true` in Program.cs.
        ```csharp
        // configure and create the carousel, register event handler
        Carousel carousel = new(config: new CarouselConfiguration()
        {
            PodCount = appParams.PodCount,
            LogInvoke = false,
            SuppressNoisyINF = false
        });
        ```
    1. Optionally update the message count
    1. run the application
    1. The Heartbeat counter (should) stall at some point, it is not 100%. If it does stall you will need to ctrl^c to terminate.
    1. set the `LogInvoke` property to `true`
    1. run the application again
    1. It should complete.

    I am still not sure exactly what is causing the issue.  check back for updates.
1. There are a few compiler warnings still, on the todo list.