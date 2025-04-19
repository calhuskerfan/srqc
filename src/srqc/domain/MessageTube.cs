using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace srqc.domain
{
    public class MessageTube : IProcessingContainer
    {
        ILogger _logger = Log.ForContext<Carousel>();

        //some internal state
        private readonly Pod[] _pods;
        private readonly MessageTubeConfig _config;
        bool _running = true;

        public event EventHandler<MessageReadyEventArgs>? MessageReadyAtExitEvent;

        private readonly EventWaitHandle StagingQueueWaitToLoadHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        private readonly EventWaitHandle ShutdownCompleteHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        internal Thread UnloadQueueProcessingThread { get; set; }

        internal ConcurrentQueue<Pod> _messageTube { get; private set; } = new ConcurrentQueue<Pod>();

        private volatile int _atExit = 0;

        public MessageTube(MessageTubeConfig config)
        {
            _config = config;

            _pods = new Pod[_config.PodCount];

            for (int i = 0; i < _config.PodCount; i++)
            {
                _pods[i] = new Pod(i);
            }

            _atExit = _config.PodCount - 1;

            UnloadQueueProcessingThread = new Thread(() => this.ProcessStagingQueueThreadFunc());
            UnloadQueueProcessingThread.Start();
        }

        public bool IsContainerEmpty()
        {
            return _messageTube.Count == 0;
        }

        private void ProcessStagingQueueThreadFunc()
        {
            while (_running)
            {
                Pod? pod;

                if (_messageTube.TryDequeue(out pod))
                {
                    StagingQueueWaitToLoadHandle.Set();

                    pod.WaitForProcessingComplete();

                    _logger.Information("Pod {idx}: Unloaded Message {id} from {driver}", pod.Idx, pod.GetMessageId(), "unload");

                    this.OnMessageReady(new MessageReadyEventArgs()
                    {
                        Message = pod.Unload()
                    });
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
                _logger.Debug("QueueMessage: Queue message {id}", message.Id);
            }

            //create a new pod every single time
            Pod p = new Pod(AtEntrance());
            //Pod p = _pods[AtEntrance()]

            p.ProcessMessage(message);
            _messageTube.Enqueue(p);

            _atExit = _atExit == 0 ? _config.PodCount - 1 : _atExit - 1;

            if (!ReadyToLoad())
            {
                StagingQueueWaitToLoadHandle.Reset();
            }
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

        public void WaitForStagingQueue()
        {
            StagingQueueWaitToLoadHandle.WaitOne();
        }

        private bool ReadyToLoad()
        {
            //lets start simple by accepting up to 5 messages in the boarding area
            if (_messageTube.Count < 7)
            {
                return true;
            }

            return false;
        }

        protected virtual void OnMessageReady(MessageReadyEventArgs e)
        {
            MessageReadyAtExitEvent?.Invoke(this, e);
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
    }
}
