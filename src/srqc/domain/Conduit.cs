using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace srqc.domain
{
    /// <summary>
    /// The conduit manages the message processing system.
    /// </summary>
    /// <remarks></remarks>
    public class Conduit : IProcessingSystem
    {
        ILogger<Conduit> _logger;

        //some internal state
        private readonly Pod[]? _pods;
        private readonly ConduitConfig _config;
        bool _running = true;

        //is this thread safe
        IClaimCheck? _nextTicket;

        // the concurrent queue that is holding the pods as they are processing
        internal ConcurrentQueue<Pod> _conduit { get; private set; } = new ConcurrentQueue<Pod>();

        public event EventHandler<MessageReadyEventArgs>? MessageReadyAtExitEvent;

        private readonly EventWaitHandle WaitToLoadHandle = new(true, EventResetMode.ManualReset);

        /*
        if we are reusing pods this queue will keep
        track of pods that are available to pick up a new message
        */
        internal ConcurrentQueue<int> WaitingToProcess = new ConcurrentQueue<int> { };

        // The Thread that is unloading the messages
        internal Thread _unloadProcessingThread { get; set; }

        // shutdown is used to safely process any inflight messages.
        private readonly EventWaitHandle ShutdownCompleteHandle = new(true, EventResetMode.ManualReset);


        /// <summary>
        /// Initialze the MessageTube
        /// </summary>
        /// <param name="config"></param>
        public Conduit(
            ILogger<Conduit> logger,
             IOptions<ConduitConfig> options)
        {
            _logger = logger;
            _config = options.Value;

            if (_config.ReUsePods)
            {
                _pods = new Pod[_config.PodCount];

                for (int i = 0; i < _config.PodCount; i++)
                {
                    _pods[i] = new Pod(i);
                    WaitingToProcess.Enqueue(i);
                }
            }

            _unloadProcessingThread = new Thread(() => this.ProcessConduitUnloadThreadFunc());
            _unloadProcessingThread.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsSystemEmpty()
        {
            return _conduit.Count == 0;
        }


        /// <summary>
        /// The ProcessConduitUnloadThreadFunc is responsible for processing the Pod at the exit of
        /// conduit.
        /// </summary>
        private void ProcessConduitUnloadThreadFunc()
        {
            while (_running || !IsSystemEmpty())
            {
                Pod? pod;

                if (_conduit.TryDequeue(out pod))
                {
                    pod.WaitForProcessingComplete();

                    _logger.LogInformation(
                        "Pod {idx:D3}: Message {id:D4} completed in {msec}", pod.Idx, pod.GetMessageId(), pod.LastExecutionTime.TotalMilliseconds);

                    //fire the event that message has been completed
                    this.OnMessageReady(new MessageReadyEventArgs()
                    {
                        Message = pod.Unload()
                    });

                    if (_config.ReUsePods)
                    {
                        WaitingToProcess.Enqueue(pod.Idx);
                    }

                    WaitToLoadHandle.Set();

                }
                else
                {
                    _logger.LogTrace("No Pods In Conduit - Sleeping");
                    Thread.Sleep(100);
                }
            }

            _logger.LogInformation("Staging Queue Thread exiting");

            ShutdownCompleteHandle.Set();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void LoadMessage(IClaimCheck claimCheck, MessageIn message)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                {
                    _logger.LogDebug("LoadMessage: Queue message {id}", message.Id);
                }

                if (claimCheck.Ticket != _nextTicket.Ticket)
                {
                    throw new InvalidOperationException();
                }

                // There are two use cases that we want to explore.  One is a new pod for every
                // message.  This is the easiest condition.
                // The second is where we want to 
                // reuse pods becuase there is significant initialization time involved.

                Pod p;

                if (_config.ReUsePods)
                {
                    if (WaitingToProcess.TryDequeue(out int nextPod))
                    {
                        _logger.LogDebug($"LoadMessage: nextpod up: {nextPod}");
                        p = _pods[nextPod];
                    }
                    else
                    {
                        throw new InvalidOperationException("Tried to Dequeue with nothing available");
                    }
                }
                else
                {
                    p = new Pod(0);
                }

                _conduit.Enqueue(p);

                p.ProcessMessage(message);

                _logger.LogDebug("Pods in Conduit {Count}", _conduit.Count);

                if (ReadyToLoad())
                {
                    //there are slots available
                    WaitToLoadHandle.Set();
                }
            }
        }

        /// <summary>
        /// Stop
        /// </summary>
        public void Stop()
        {
            _running = false;

            ShutdownCompleteHandle.Reset();
            ShutdownCompleteHandle.WaitOne();

            _logger.LogInformation("Conduit Stop Complete");
        }

        /// <summary>
        /// WaitForStagingQueueSlotAvailable is called by the message producer queue handler
        /// to verify that there is a spot in the staging queue.
        /// </summary>
        /// <remarks>We still need to work out how to tell the producing system that
        /// we are no longer accepting claim check requests.</remarks>
        public IClaimCheck WaitForProcessingSlotAvailable()
        {
            WaitToLoadHandle.WaitOne();
            WaitToLoadHandle.Reset();
            _nextTicket = new ClaimCheck() { Ticket = new Guid()};
            return _nextTicket;
        }

        /// <summary>
        /// ReadyToLoad
        /// </summary>
        /// <returns></returns>
        private bool ReadyToLoad()
        {
            if (_config.ReUsePods)
            {
                return !WaitingToProcess.IsEmpty;
            }
            else
            {
                // a little arbitrary for now.
                return _conduit.Count < _config.PodCount;
            }
        }

        /// <summary>
        /// ReadyToLoad
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnMessageReady(MessageReadyEventArgs e)
        {
            MessageReadyAtExitEvent?.Invoke(this, e);
        }
    }
}
