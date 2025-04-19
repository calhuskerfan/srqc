using Serilog;
using System.Collections.Concurrent;
using System.Text;

namespace srqc.domain
{
    public class Carousel : IProcessingContainer
    {
        ILogger _logger = Log.ForContext<Carousel>();

        //some internal state
        private readonly Pod[] _pods;
        private volatile int _atExit = 0;
        bool _running = true;
        private readonly CarouselConfiguration _config;

        // Informs listener that a message is ready
        public event EventHandler<MessageReadyEventArgs>? MessageReadyAtExitEvent;

        // internal synchronization
        private readonly EventWaitHandle StagingQueueWaitToLoadHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        private readonly EventWaitHandle InternalReadyToLoadHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        private readonly EventWaitHandle ShutdownCompleteHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        private readonly object _rotateLock = new();


        //staging queue and processing
        internal ConcurrentQueue<MessageIn> StagingQueue { get; private set; } = new ConcurrentQueue<MessageIn>();
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

        // load messages from the staging queue.
        private void ProcessStagingQueueThreadFunc()
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

            ShutdownCompleteHandle.Set();
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

            // since we have now rotated we have an open spot, set event for Staging Queue Thread
            InternalReadyToLoadHandle.Set();

            // if a pod shows up at the exit running we want to free the Staging Queue Thread to start another message
            // running.  so spawn a thread to wait for pod to finish and complete the rotate.
            if (_pods[AtExit()].State == PodState.Running)
            {
                new Thread((object? managedThreadId) =>
                {
                    if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
                    {
                        _logger.Debug("Pod {idx}: Wait for Processing to complete.  ParentThread {threadId}", AtExit(), managedThreadId == null ? 0 : (int)managedThreadId);
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
            // rotates the last of the messages off of the carousel if there
            // are no messages waiting in the staging queue
            if (StagingQueue.Count == 0
                && _pods[AtEntrance()].State == PodState.WaitingToLoad
                && _pods[AtExit()].State == PodState.WaitingToLoad
                && !IsContainerEmpty())
            {
                _logger.Information("Recurse");
                Rotate("Recurse");
            }

            return true;
        }


        public void Stop()
        {
            //this needs some work.  a boolean is ok for now, in 
            //reality we would want to wait for any cleanup, messages to finish, etc.
            _running = false;

            ShutdownCompleteHandle.Reset();
            ShutdownCompleteHandle.WaitOne();

            _logger.Information("Stop Complete");
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

        // needs a little work, relies on inbound queue side to self regulate
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
                StagingQueueWaitToLoadHandle.Reset();
            }
        }

        // fire message ready event
        protected virtual void OnMessageReady(MessageReadyEventArgs e)
        {
            // this should be removed once we finalize the issue with 'message recevied' order.  see readme
            if (_config.LogInvoke)
            {
                _logger.Information("Pod {idx}: Invoke Message Ready {messageId}", e.Message.ProcessedByPod, e.Message.Id);
            }

            MessageReadyAtExitEvent?.Invoke(this, e);
        }

        //we cannot rotate if the pod at exit is running or ready to unload
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
        private int AtEntrance()
        {
            return _atExit == _config.PodCount - 1 ? 0 : _atExit + 1;
        }

        //what pod is at the exit
        private int AtExit()
        {
            return _atExit;
        }

        // empty if all pods in WaitingToLoad State
        public bool IsContainerEmpty()
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
