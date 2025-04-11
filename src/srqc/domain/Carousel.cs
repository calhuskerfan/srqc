using Serilog;
using System.Collections.Concurrent;
using System.Text;

namespace srqc.domain
{
    public class Carousel
    {
        ILogger _logger = Log.ForContext<Carousel>();

        //some internal state
        private List<Pod> _pods;
        private volatile int _atExit = 0;
        bool _running = true;
        CarouselConfiguration _config;

        // Informs listener that a message is ready
        public event EventHandler<MessageReadyEventArgs>? MessageReadyAtExitEvent;

        // internal synchronization
        private EventWaitHandle StagingQueueWaitToLoadHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        private EventWaitHandle InternalReadyToLoadHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        private RotateLock _rotationLock = new RotateLock();


        //staging queue and processing
        public ConcurrentQueue<MessageIn> StagingQueue { get; private set; } = new ConcurrentQueue<MessageIn>();
        internal Thread StagingQueueProcessingThread { get; set; }

        // the pods
        private Pod[] Pods { get { return _pods.ToArray<Pod>(); } }


        public Carousel(CarouselConfiguration config)
        {
            _config = config;

            _pods = new List<Pod>();

            for (int i = 0; i < _config.PodCount; i++)
            {
                Pod p = new Pod(i);
                _pods.Add(p);
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

                    // TODO: We could make this an autoreset and save ourselves a call here.
                    InternalReadyToLoadHandle.WaitOne();
                    InternalReadyToLoadHandle.Reset();

                    _logger.Information("Pod {pod}: Load Message {id}", AtEntrance(), message.Id);

                    Pods[AtEntrance()].ProcessMessage(message);

                    _rotationLock.AcquireLock();
                    if (!Rotate("queue-processor"))
                    {
                        // NOTE: so if rotate failed here we are going to assume that there is another one coming,
                        // we could also, 'look ahead' and never call, but they are effectively the same thing
                        // the thing to come back and re-visit is are there any conditions where this stalls
                        // the loading
                        // log as debug since it is not 'abnormal'
                        if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
                        {
                            _logger.Debug("Failed to Rotate");
                        }
                    }
                    _rotationLock.ReleaseLock();
                }
                else
                {
                    _logger.Information("Nothing in Boarding Queue - Sleeping");
                    Thread.Sleep(250);
                }
            }

            _logger.Information("Boarding Queue Thread exiting");
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

            // since we have now rotated we have an open spot
            InternalReadyToLoadHandle.Set();

            if (Pods[AtExit()].State == PodState.ReadyToUnload)
            {
                this.OnMessageReady(new MessageReadyEventArgs()
                {
                    Message = Pods[AtExit()].Unload()
                });

                _logger.Information("Pod {idx}: Unloaded Message {id} from {driver}", AtExit(), Pods[AtExit()].GetMessageId(), "rotate");
            }

            if (Pods[AtExit()].State == PodState.Running)
            {
                new Thread(() =>
                {
                    if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
                    {
                        _logger.Debug("Pod {idx}: Wait for Processing to complete", AtExit());
                    }

                    Pods[AtExit()].WaitForProcessingComplete();

                    this.OnMessageReady(new MessageReadyEventArgs()
                    {
                        Message = Pods[AtExit()].Unload()
                    });

                    _logger.Information("Pod {idx}: Unloaded Message {id} from {driver}", AtExit(), Pods[AtExit()].GetMessageId(), "wait to unload");

                    _rotationLock.AcquireLock();
                    Rotate("thread");
                    _rotationLock.ReleaseLock();

                }).Start();

                return true;
            }

            // still not completely happy with this.
            // the main goal is to quickly rotate the last of the messages off of the carousel if there
            // are no messages waiting in the staging queue
            if (StagingQueue.Count == 0
                && Pods[AtEntrance()].State == PodState.WaitingToLoad
                && Pods[AtExit()].State == PodState.WaitingToLoad
                && !IsCarouselEmpty())
            {
                if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
                {
                    _logger.Debug("Recurse");
                }
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
            MessageReadyAtExitEvent?.Invoke(this, e);
        }

        //we cannot rotate if the pod at exit is running or waiting to unload
        protected bool CanRotate()
        {
            if (Pods[AtExit()].State == PodState.Running
                || Pods[AtExit()].State == PodState.ReadyToUnload)
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
            foreach (var item in Pods)
            {
                if (item.State != PodState.WaitingToLoad)
                {
                    return false;
                }
            }

            return true;
        }

        // a string that can be used to visualize the state of the carouse;
        private string GetStatus()
        {
            StringBuilder sb = new StringBuilder();
            int f = AtEntrance();
            for (int i = 0; i < Pods.Length; i++)
            {
                sb.Append(Pods[f].ToString());
                sb.Append("-->");

                f++;
                if (f >= Pods.Length)
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
