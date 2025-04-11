using Serilog;
using System.Collections.Concurrent;
using System.Text;

namespace srqc.domain
{
    public class Carousel
    {
        ILogger _logger = Log.ForContext<Carousel>();

        //some internal state
        private Pod[] _pods;
        private volatile int _atExit = 0;
        bool _running = true;
        CarouselConfiguration _config;

        // Informs listener that a message is ready
        public event EventHandler<MessageReadyEventArgs>? MessageReadyAtExitEvent;

        // internal synchronization
        private EventWaitHandle StagingQueueWaitToLoadHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        private EventWaitHandle InternalReadyToLoadHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        private readonly object _rotateLock = new();


        //staging queue and processing
        public ConcurrentQueue<MessageIn> StagingQueue { get; private set; } = new ConcurrentQueue<MessageIn>();
        internal Thread StagingQueueProcessingThread { get; set; }

        public Carousel(CarouselConfiguration config)
        {
            _config = config;

            _pods = new Pod[_config.PodCount];

            for (int i = 0; i < _config.PodCount; i++)
            {
                _pods[i] = new Pod(i);
            }

            _atExit = _config.PodCount - 1;

            _logger.Information("Carousel created with {podcount} pods", _config.PodCount);

            StagingQueueProcessingThread = new Thread(() => this.ProcessStagingQueueThreadFunc());
            StagingQueueProcessingThread.Start();
        }

        public void ProcessStagingQueueThreadFunc()
        {
            while (_running)
            {
                MessageIn? message;

                if (StagingQueue.TryDequeue(out message))
                {
                    StagingQueueWaitToLoadHandle.Set();

                    InternalReadyToLoadHandle.WaitOne();
                    InternalReadyToLoadHandle.Reset();

                    int entryLoadedIdx = AtEntrance();
                    _pods[entryLoadedIdx].ProcessMessage(message);

                    if (!_config.SuppressNoisyINF)
                    {
                        _logger.Information("Pod {pod}: Load Message {id}", entryLoadedIdx, message.Id);
                    }

                    lock (_rotateLock)
                    {
                        //if pod we loaded is still at the entrance, then we can rotate, otherwise
                        //a thread callback did our work for us
                        if (entryLoadedIdx == AtEntrance())
                        {
                            if (!Rotate("queue-processor"))
                            {
                                if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
                                {
                                    _logger.Debug($"Failed to Rotate from queue-processor {GetStatus()}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    _logger.Information("Nothing in Staging Queue - Sleeping");
                    Thread.Sleep(250);
                }
            }

            _logger.Information("Staging Queue Thread exiting");
        }

        // rotate the carousel
        protected bool Rotate(string from)
        {
            if (!CanRotate())
            {
                return false;
            }

            // Rotate
            _atExit = _atExit == 0 ? _config.PodCount - 1 : _atExit - 1;

            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                _logger.Debug("Post-Rotate {from}.  {status}", from, GetStatus());
            }

            if (_pods[AtExit()].State == PodState.ReadyToUnload)
            {
                this.OnMessageReady(new MessageReadyEventArgs()
                {
                    Message = _pods[AtExit()].Unload()
                });

                if (!_config.SuppressNoisyINF)
                {
                    _logger.Information("Pod {idx}: Unloaded Message {id} from {driver}", AtExit(), _pods[AtExit()].GetMessageId(), "rotate");
                }
            }

            // since we have now rotated we have an open set event handle for Staging Queue Thread
            InternalReadyToLoadHandle.Set();

            // if a pod shows up at the exit running we want to free the Staging Queue Thread to start another message
            // running.  so spawn a thread to wait for pod to finish.
            if (_pods[AtExit()].State == PodState.Running)
            {
                new Thread((object managedThreadId) =>
                {
                    if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
                    {
                        _logger.Debug("Pod {idx}: Wait for Processing to complete.  ParentThread {threadId}", AtExit(), (int)managedThreadId);
                    }

                    _pods[AtExit()].WaitForProcessingComplete();

                    lock (_rotateLock)
                    {
                        //wait till we get the lock to actually unload.
                        this.OnMessageReady(new MessageReadyEventArgs()
                        {
                            Message = _pods[AtExit()].Unload()
                        });

                        if (!_config.SuppressNoisyINF)
                        {
                            _logger.Information("Pod {idx}: Unloaded Message {id} from {driver}", AtExit(), _pods[AtExit()].GetMessageId(), "wait to unload");
                        }

                        if (!Rotate("callback"))
                        {
                            // NOTE: we should never get here.  ok to remove.
                            _logger.Error($"Failed to Rotate from callback {GetStatus()}");
                        }
                    }

                }).Start(System.Threading.Thread.CurrentThread.ManagedThreadId);

                return true;
            }

            // still not completely happy with this.
            // the main goal is to quickly rotate the last of the messages off of the carousel if there
            // are no messages waiting in the staging queue
            if (StagingQueue.Count == 0
                && _pods[AtEntrance()].State == PodState.WaitingToLoad
                && _pods[AtExit()].State == PodState.WaitingToLoad
                && !IsCarouselEmpty())
            {
                _logger.Information("Recurse");
                Rotate("Recurse");
            }

            return true;
        }


        public void Stop()
        {
            //this really needs some work
            _running = false;
            Thread.Sleep(300);
        }

        //blocks loading thread, we could move this internally to 
        //Load message ?
        public void WaitForStagingQueue()
        {
            StagingQueueWaitToLoadHandle.WaitOne();
        }

        private bool ReadyToLoad()
        {
            //lets start simple by accepting up to 5 messages in the boarding area
            if (StagingQueue.Count < _config.BoardingQueueSize)
            {
                return true;
            }

            return false;
        }

        // needs a little work, since we rely on inbound queue side to self regulate
        // by calling WaitForStagingQueue
        // we could 'block' here instead
        public void LoadMessage(MessageIn message)
        {
            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                _logger.Debug("QueueMessage: Queue message {id}", message.Id);
            }

            StagingQueue.Enqueue(message);

            if (!ReadyToLoad())
            {
                if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
                {
                    _logger.Verbose("Not Ready to Load, Close Staging Queue");
                }

                StagingQueueWaitToLoadHandle.Reset();
            }
        }

        // fire message ready event
        protected virtual void OnMessageReady(MessageReadyEventArgs e)
        {
            if (_config.LogInvoke)
            {
                _logger.Information("Pod {idx}: Invoke Message Ready {messageId}", e.Message.ProcessedByPod, e.Message.Id);
            }

            MessageReadyAtExitEvent?.Invoke(this, e);
        }

        //we cannot rotate if the pod at exit is running or waiting to unload
        protected bool CanRotate()
        {
            if (_pods[AtExit()].State == PodState.Running
                || _pods[AtExit()].State == PodState.ReadyToUnload)
            {
                return false;
            }

            return true;
        }

        //what pod is at the entrance
        public int AtEntrance()
        {
            return _atExit == _config.PodCount - 1 ? 0 : _atExit + 1;
        }

        //what pod is at the exit
        public int AtExit()
        {
            return _atExit;
        }

        // empty if all pods in WaitingToLoad State
        public bool IsCarouselEmpty()
        {
            for (int i = 0; i < _pods.Length; i++)
            {
                if (_pods[i].State != PodState.WaitingToLoad)
                {
                    return false;
                }
            }

            return true;
        }

        // a string that can be used to visualize the state of the carousel;
        private string GetStatus()
        {
            StringBuilder sb = new StringBuilder();
            int f = AtEntrance();
            for (int i = 0; i < _pods.Length; i++)
            {
                sb.Append(_pods[f].ToString());
                sb.Append("[");
                sb.Append(_pods[f].GetMessageId());
                sb.Append("]");
                sb.Append("-->");

                f++;
                if (f >= _pods.Length)
                {
                    f = 0;
                }
            }
            string retstr = sb.ToString();

            //take out the last '-->'
            return retstr[..^3];
        }
    }
}
