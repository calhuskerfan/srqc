using Serilog;
using System.Collections.Concurrent;

namespace srqc.domain
{
    public class Conduit : IProcessingContainer
    {
        ILogger _logger = Log.ForContext<Conduit>();

        //some internal state
        private readonly Pod[] _pods;
        private readonly ConduitConfig _config;
        bool _running = true;
        private volatile int _atExit = 0;

        public event EventHandler<MessageReadyEventArgs>? MessageReadyAtExitEvent;

        internal ConcurrentQueue<MessageIn> MessageStagingQueue = new ConcurrentQueue<MessageIn> { };
        private readonly EventWaitHandle MessageStagingQueueWaitToLoadHandle = new(true, EventResetMode.ManualReset);

        internal ConcurrentQueue<int> WaitingToProcess = new ConcurrentQueue<int> { };

        private readonly EventWaitHandle ShutdownCompleteHandle = new(true, EventResetMode.ManualReset);

        internal Thread UnloadQueueProcessingThread { get; set; }

        internal ConcurrentQueue<Pod> _conduit { get; private set; } = new ConcurrentQueue<Pod>();
        private readonly EventWaitHandle MessageProcessingTubeWaitToLoadHandle = new(true, EventResetMode.ManualReset);


        /// <summary>
        /// Initialze the MessageTube
        /// </summary>
        /// <param name="config"></param>
        public Conduit(ConduitConfig config)
        {
            _config = config;

            if (config.ReUsePods)
            {
                _pods = new Pod[_config.PodCount];

                for (int i = 0; i < _config.PodCount; i++)
                {
                    _pods[i] = new Pod(i);
                    WaitingToProcess.Enqueue(i);
                }
            }

            UnloadQueueProcessingThread = new Thread(() => this.ProcessStagingQueueThreadFunc());
            UnloadQueueProcessingThread.Start();
        }

        public bool IsContainerEmpty()
        {
            return _conduit.Count == 0;
        }


        // thread to dequeue pods at the end of the tube.
        private void ProcessStagingQueueThreadFunc()
        {
            while (_running || !IsContainerEmpty())
            {
                Pod? pod;

                if (_conduit.TryDequeue(out pod))
                {
                    // There is another spot available in the staging queue
                    //MessageStagingQueueWaitToLoadHandle.Set();

                    pod.WaitForProcessingComplete();

                    _logger.Information("Pod {idx}: Unloaded Message {id} from {driver}", pod.Idx, pod.GetMessageId(), "unload");

                    this.OnMessageReady(new MessageReadyEventArgs()
                    {
                        Message = pod.Unload()
                    });

                    if (_config.ReUsePods)
                    {
                        WaitingToProcess.Enqueue(pod.Idx);
                    }

                    MessageStagingQueueWaitToLoadHandle.Set();

                }
                else
                {
                    _logger.Information("Nothing in Staging Queue - Sleeping");
                    Thread.Sleep(100);
                }
            }

            _logger.Information("Staging Queue Thread exiting");

            ShutdownCompleteHandle.Set();
        }


        public void LoadMessage(MessageIn message)
        {
            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                _logger.Debug("LoadMessage: Queue message {id}", message.Id);
            }

            // There are two use cases that we want to explore.  One is a new pod for every
            // message.  This is the easiest condition.
            // The second is where we want to 
            // reuse pods becuase there is significant initialization time involved.

            Pod p;

            //create a new pod every single time
            if (_config.ReUsePods)
            {
                if (WaitingToProcess.TryDequeue(out int nextPod))
                {
                    _logger.Debug($"nextpod: {nextPod}");
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

            p.ProcessMessage(message);

            _conduit.Enqueue(p);

            _logger.Debug($"{_conduit.Count}");

            if (ReadyToLoad())
            {
                MessageStagingQueueWaitToLoadHandle.Set();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            //this needs some work.  a boolean is ok for now, in 
            //reality we would want to wait for any cleanup, messages to finish, etc.
            _running = false;

            ShutdownCompleteHandle.Reset();
            ShutdownCompleteHandle.WaitOne();

            _logger.Information("Stop Complete");
        }

        /// <summary>
        /// WaitForStagingQueue
        /// </summary>
        public void WaitForStagingQueue()
        {
            MessageStagingQueueWaitToLoadHandle.WaitOne();
            MessageStagingQueueWaitToLoadHandle.Reset();
        }

        // ReadyToLoad
        private bool ReadyToLoad()
        {
            if (_config.ReUsePods)
            {
                return WaitingToProcess.Count > 0;
            }
            else
            {
                // a little arbitrary for now.
                return _conduit.Count < _config.PodCount;
            }
        }

        protected virtual void OnMessageReady(MessageReadyEventArgs e)
        {
            MessageReadyAtExitEvent?.Invoke(this, e);
        }
    }
}
