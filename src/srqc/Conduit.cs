using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Srqc
{
#pragma warning disable CS8601
#pragma warning disable CS8602

    /// <summary>
    /// The conduit manages the message processing system.
    /// </summary>
    /// <remarks></remarks>
    public class Conduit<TMessageIn, TMessageOut> : IProcessingSystem<TMessageIn, TMessageOut>
    {
        private readonly ILogger<Conduit<TMessageIn, TMessageOut>> _logger;
        private readonly ConduitConfig _config;
        private readonly ITransformerFactory<TMessageIn, TMessageOut> _transformerFactory;

        //some internal state
        private readonly Pod<TMessageIn, TMessageOut>[]? _pods;

        bool _running = true;

        //is this thread safe
        IClaimCheck? _nextTicket;

        // the concurrent queue that is holding the pods as they are processing
        internal ConcurrentQueue<Pod<TMessageIn, TMessageOut>> _conduit { get; private set; } = new ConcurrentQueue<Pod<TMessageIn, TMessageOut>>();

        public event EventHandler<MessageReadyEventArgs <TMessageOut>>? MessageReadyAtExitEvent;

        private readonly EventWaitHandle WaitToLoadHandle = new(true, EventResetMode.ManualReset);

        /*
        if we are reusing pods this queue will keep
        track of pods that are available to pick up a new message
        */
        internal ConcurrentQueue<int> PodsAvailableForProcessing = new ConcurrentQueue<int> { };

        internal Thread _unloadProcessingThread { get; set; }

        // shutdown is used to safely process any inflight messages.
        private readonly EventWaitHandle ShutdownCompleteHandle = new(true, EventResetMode.ManualReset);


        /// <summary>
        /// Initialze the Conduit
        /// </summary>
        /// <param name="config"></param>
        public Conduit(
            ILogger<Conduit<TMessageIn, TMessageOut>> logger,
            IOptions<ConduitConfig> options,
            ITransformerFactory<TMessageIn, TMessageOut> transformerFactory)
        {
            _logger = logger;
            _config = options.Value;
            _transformerFactory = transformerFactory;

            if (_config.ReUsePods)
            {
                _pods = new Pod<TMessageIn, TMessageOut>[_config.PodCount];

                for (int i = 0; i < _config.PodCount; i++)
                {
                    var pod = new Pod<TMessageIn, TMessageOut>(i) { Go = _transformerFactory.GetTransformer() };
                    _pods[i] = pod;
                    PodsAvailableForProcessing.Enqueue(i);
                }
            }

            _unloadProcessingThread = new Thread(() => ProcessConduitUnloadThreadFunc());
            _unloadProcessingThread.Start();
        }

        /// <summary>
        /// returns true if there are no pods in the conduit
        /// </summary>
        /// <returns></returns>
        public bool IsSystemEmpty()
        {
            return _conduit.Count == 0;
        }


        /// <summary>
        /// The ProcessConduitUnloadThreadFunc is responsible for processing the Pod
        /// at the exit of conduit.
        /// </summary>
        private void ProcessConduitUnloadThreadFunc()
        {
            _logger.LogInformation("ProcessConduitUnloadThreadFunc Starting");

            while (_running || !IsSystemEmpty())
            {
                Pod<TMessageIn, TMessageOut>? pod;

                if (_conduit.TryDequeue(out pod))
                {
                    pod.WaitForProcessingComplete();

                    _logger.LogInformation(
                        "Pod {idx:D3}: Completed in {msec}", pod.Idx, pod.LastExecutionTime.TotalMilliseconds);

                    //fire the event that message has been completed
                    OnMessageReady(new MessageReadyEventArgs<TMessageOut>()
                    {
                        Message = pod.Unload(),
                        RuntimeMsec = (int)pod.LastExecutionTime.TotalMilliseconds,
                        ProcessedByPod = pod.Id,
                        ProcessedByPodIdx = pod.Idx
                    });

                    if (_config.ReUsePods)
                    {
                        PodsAvailableForProcessing.Enqueue(pod.Idx);
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
        public void LoadMessage(IClaimCheck claimCheck, TMessageIn message)
        {
            var msg = message;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("LoadMessage: claimcheck {id}", claimCheck.Ticket);
            }

            if (claimCheck.Ticket != _nextTicket.Ticket)
            {
                throw new InvalidOperationException("Invalid Claim Check");
            }

            // There are two use cases that we want to explore.  One is a new pod for every
            // message.  This is the easiest condition.
            // The second is where we want to 
            // reuse pods becuase there is significant initialization time involved.

            Pod<TMessageIn, TMessageOut> p;

            if (_config.ReUsePods)
            {
                if (PodsAvailableForProcessing.TryDequeue(out int nextPod))
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
                p = new Pod<TMessageIn, TMessageOut>(0) { Go = _transformerFactory.GetTransformer() };
            }

            _conduit.Enqueue(p);

            p.ProcessMessage(msg);

            _logger.LogDebug("Pods in Conduit {Count}", _conduit.Count);

            if (ReadyToLoad())
            {
                //there are slots available
                WaitToLoadHandle.Set();
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
        /// WaitForProcessingSlotAvailable is called by the message producer queue handler
        /// to verify that there is a spot in the staging queue.
        /// </summary>
        /// <remarks>
        /// We still need to work out how to tell the producing system that
        /// we are no longer accepting claim check requests.
        /// </remarks>
        public IClaimCheck WaitForProcessingSlotAvailable()
        {
            WaitToLoadHandle.WaitOne();
            WaitToLoadHandle.Reset();
            _nextTicket = new ClaimCheck();
            return _nextTicket;
        }

        /// <summary>
        /// True if there are pods available to load
        /// </summary>
        /// <returns></returns>
        private bool ReadyToLoad()
        {
            if (_config.ReUsePods)
            {
                return !PodsAvailableForProcessing.IsEmpty;
            }
            else
            {
                // a little arbitrary for now.
                return _conduit.Count < _config.PodCount;
            }
        }

        /// <summary>
        /// OnMessageReady
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnMessageReady(MessageReadyEventArgs<TMessageOut> e)
        {
            MessageReadyAtExitEvent?.Invoke(this, e);
        }
    }

#pragma warning restore CS8602
#pragma warning restore CS8601

}
