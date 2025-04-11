# SR Queue Consumer

The SR Queue Consumer is a project that explores a message queue consumer processing pattern to increase throughput of message transformations in a First In First Out (FIFO) manner.  It was inspired by the intersection of a problem I was working on and an article on the loading design for Disney's Millennium Falcon: Smugglers Run attraction.

This document assumes some level of familiarity with queues, message processing, and message orientated middleware concepts.

## Background

Millennium Falcon: Smugglers Run is an interactive motion simuator ride that puts a crew of 6 riders into the cockpit of the Millennium Falcon.  An imaginative component of the ride is its loading and segmentation which allow for high throughput of riders (lower wait times) and an immersive loading experience where each crew waits their turn in the chess room of the millenium falcon.  To create the expereince there are actually 4 ride turntables each with 7 pods, where each pod encapusulates the ride experience for each crew.

This [article](https://www.bizjournals.com/orlando/news/2019/10/04/how-it-works-patent-behind-disneys-millennium.html) has a detailed description (appologize if you hit a paywall).  There is another, shorter description [here](https://disneydiary.com/2019/10/new-disney-patent-shows-how-the-millennium-falcon-ride-works/).  Finally, the [patent](https://patents.google.com/patent/EP3628383A1/en) is also available.

## Introduction

As luck would have it I had that patent somewhere in my mind while I was working on some throughput issues for a FIFO queue.  In my case the per message transformation time was the primary bottleneck in the system.

There are a few general [patterns for this](https://en.wikipedia.org/wiki/Message_queue), two of the most popular being.

1. Process messages using competing consumer pattern with a SequenceId that re-assembles the order in a post processing step.
1. Using some form of partitioning structure within the queue to parallel messages.  While this increases overall speed of a population of messages any messages assigned to a particular partition are still only processed one at a time.

In my case I did not want to worry about the re-assembly and wanted faster 'per partition' processing.

By applying a segmented turntable construct we are able to increase the system throughput, maintain FIFO order, and eliminate any re-assembly when messages are complete.

##  System Overview

The following is a diagram of the SR Queue Consumer system

![Alt text here](srloader.drawio.svg)


The sytems contains a virutal carousel with a configurable number of pods. Pods operate on a message via a thread assigned to the pod.  The system loads and unloads the pods in order. As pods are loaded and unloaded they are 'rotated' to allow the next message to start processing.

The primary system components are:

| Component | Description |
| --------- | ----------- |
| Inbound Queue | A typical inbound queue in a Message Orientated Middleware (MOM) style application. |
| Staging Area | The staging area is a small internal queue that helps to optimize the loading and unloading operations. |
| Message Loading | Message loading monitors the staging area and the carousel state to place waiting messages from the staging area into a pod for processing. |
| Pod | A pod is a container that processes the message from inbound to outbound format on a thread. |
| Carousel | The carousel serves as an organizational construct for where messages are in their processing order. |
| Message Unloading | When a pod is 'rotated' to the exit it is unloaded.  If the pod is still running it will wait for completion before advancing. |
| Outbound Queue  | Messages that have completed processing are placed in the outbound queue. |

So hypothetically if a message takes 250 msec to process then our maximum message rate is 4 messages / sec if processed in serial manner, however if we can process 3 at a time we increase our rate to 12 messages / sec.  While we will not see the theoritical throughput based on management overhead and message timing variation we will see a significnat improvement.

### Operation

The system operates in the following manner

1. Messages are added to the inbound queue by a message producer
1. Messages are admitted into the staging queue and it is monitored for capacity.
1. Messages are loaded into a pod at the entry station, message processing thread is started, and the carousel is rotated.
1. This process continues until the message is rotated to the exit.  If the processing is completed the message is unloaded.  If the message reaches the exit before it is finished the system will pause until the procecssing is complete.
1. The message is delivered to the exit queue.
1. The carousel is rotated so that the now empty pod is moved to the entry to pick up another message
1. A new message is loaded, a spot is opened on the staging queue, and another message enters from the inbound queue.
1. The cycle continues.

### Benefits

Some benefits to this design include:

1. Improved througput when the message processing time is significant.  Either through complex processing or waiting on I/O or services.
1. The process is self contained.

### Limitations.

A few notable limitations to the system.

1.  There are diminishing returns as the message process time reduces.  Eventually the overhead of thread synchronization hampers performance.
1.  More managment of 'in process messages'.  By allowing [number of pods] + [staging area size] messages into the system at a time we lose some of the message resielence the inbound queue provides.
1.  The transformation of the message must be self contained, I.e. the only target is the exit queue.  If any portion of the processing had external (database update for example) side effects that also had FIFO requirements you could not use this pattern as there is no control of operational sequence while pds are processing.

## Running the project.

For the demonstration project the message transformation is simply a string update and a Thread.Sleep to simulate processing time.

Pre-requsites
1. git
1. dotnet9
1. VSCode or Visual Studio

Perform the following steps to execute the project
1. sync the repo
1. dotnet run --project ./src/srqc -c Release

After logging information about the operations involved in processing the output will look something like the following (timestamps and log level ommitted)
```text
010001:000:0000169
...
010150:001:0000100
total processing time:8545.3955 msec.  Accumulator 21908 msec.  2.563719841872737
```

where the first column is the message id, the second column is the pod that processed the message, and the third is the **configured** delay.  Total processing time is the start to finish of the execution.  Accumulator is the ideal time if all the messages were processed serially with no intra processing delays.


You can experiment with different parameters found at the top of Program.cs, the defaults are:
```csharp
ApplicationParameters appParams = new()
{
        PodCount = 3,
        MessageCount = 150,
        MinProcessingDelay = 100,
        MaxProcessingDelay = 200
};
```
| parameter | description |
| --------- | ----------- |
| PodCount | Number of pods assigned to the Carousel |
| MessageCount | Number of Messages to process |
| MinProcessingDelay | The minimum simulation delay assigned to a message |
| MinProcessingDelay | The maximum simulation delay assigned to a message |

## Next Steps

The envisioned system did not make its way into a production product, so many things were left as "I will get to those".  A few include:

1. Externalize the Types and logic for the pod and carousel.  By templatizing the types for inbound and outbound messages and externalizing the pod processing logic the system can be re-used.
1. Tighten up the thread and event synchronization.
1. Error Handling.  What happens when a message fails, what should ripple upstream.
1. Change the running boolean to an event handle.
1. Run as a service.
1. Hook the system up to a real message queue, I.e. rabbitMQ.
1. Add a multiple carousel construct.  This could pair up to a partitioned queue to create greater overall system performance and resielency.


## Summary

In the end this project was primarly a thought exercise, however it was succesful with the requirements that I had envisioned for it, namely:
1. speeding up parallel processing of a FIFO queue where the bottleneck was the message processing itself.
2. not adding any external complexities to maintaing message order.

If nothing else it was an opportunity to take a look at a problem with a new lens and see what comes out of it.

Your mileage may vary.