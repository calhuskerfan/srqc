using Serilog;
using System.Diagnostics;

namespace Srqc
{
    public enum PodState : int
    {
        NotInitialized,
        WaitingToLoad,
        Loading,
        Running,
        Completed,
        InError,
        ReadyToUnload,
        Unloading,
        Undefined
    }

    public class Pod : IProcessingContainer
    {
        readonly ILogger _logger = Log.ForContext<Pod>();

        public Guid Id { get; } = Guid.NewGuid();

        public int Idx { get; private set; }

        private volatile int _podstate = (int)PodState.NotInitialized;

        public TimeSpan LastExecutionTime { get; private set; }


        public PodState State
        {
            get { return (PodState)_podstate; }
            set
            {
                // volatile should suffice, but leave for now.
                Interlocked.Exchange(ref _podstate, (int)value);
            }
        }

        // internal members
        private MessageOut? _message = null;

        private EventWaitHandle ProcessingCompleteHandle = new(true, EventResetMode.ManualReset);

        public Pod(int idx)
        {
            Idx = idx;
            State = PodState.WaitingToLoad;
        }

        //
        public void ProcessMessage(MessageIn msg)
        {
            if (State != PodState.WaitingToLoad)
            {
                throw new InvalidOperationException($"Pod {Idx} is in State {State}");
            }

            State = PodState.Loading;

            Thread ProcessingThread = new Thread(() => ProcessThreadFunc(msg));
            ProcessingThread.Start();
        }

        internal void ProcessThreadFunc(MessageIn msg)
        {
            ProcessingCompleteHandle.Reset();

            LastExecutionTime = TimeSpan.Zero;

            Stopwatch sw = Stopwatch.StartNew();

            State = PodState.Running;

            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                _logger.Debug("ProcessThreadFunc in Pod {idx} has started for message {id} with delay of {msec}", Idx, msg.Id, msg.ProcessingMsec);
            }

            _message = new MessageOut()
            {
                Text = $"New outbound message is: {msg.Text} from pod {Id}",
                Id = msg.Id + 10000,
                MessageInId = msg.Id,
                RuntimeMsec = msg.ProcessingMsec,
                ProcessedByPod = Id,
                ProcessedByPodIdx = Idx
            };

            Thread.Sleep(msg.ProcessingMsec);

            State = PodState.ReadyToUnload;

            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                _logger.Debug("Pod {idx:D3} ProcessThreadFunc has Completed {state}", Idx, State);
            }

            sw.Stop();
            LastExecutionTime = sw.Elapsed;

            ProcessingCompleteHandle.Set();
        }

        //
        public int GetMessageId()
        {
            return _message == null ? 0 : _message.MessageInId;
        }


        /// <summary>
        /// Unlpoad the processed message from the pod.
        /// </summary>
        /// <returns></returns>
        public MessageOut? Unload()
        {
            if (_message == null)
            {
                _logger.Warning("Pod {idx} has no message to unload", Idx);
                State = PodState.WaitingToLoad;
                return null;
            }

            MessageOut ret = _message.Clone();
            _message = null;

            State = PodState.WaitingToLoad;
            return ret;
        }

        // 
        public override string ToString()
        {
            return $"{Idx}:{State}";
        }

        //
        public void WaitForProcessingComplete()
        {
            ProcessingCompleteHandle.WaitOne();
        }
    }
}
