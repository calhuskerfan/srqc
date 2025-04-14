using Serilog;

namespace srqc.domain
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

    public class Pod
    {
        ILogger _logger = Log.ForContext<Pod>();

        public Guid Id { get; } = Guid.NewGuid();
        public int Idx { get; private set; }

        private volatile int _podstate = (int)PodState.NotInitialized;


        public PodState State
        {
            get { return (PodState)_podstate; }
            set
            {
                // volatile should suffice, but leave for now.
                System.Threading.Interlocked.Exchange(ref _podstate, (int)value);
            }
        }

        // internal members
        private MessageOut? _message = null;

        //
        private EventWaitHandle ProcessingCompleteHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        public Pod(int idx)
        {
            Idx = idx;
            State = PodState.WaitingToLoad;
        }

        public void ProcessMessage(MessageIn msg)
        {
            if (State != PodState.WaitingToLoad)
            {
                throw new InvalidOperationException($"pod {Idx} is in state {State}");
            }

            State = PodState.Loading;

            Thread ProcessingThread = new Thread(() => ProcessThreadFunc(msg));
            ProcessingThread.Start();
        }

        //
        internal void ProcessThreadFunc(MessageIn msg)
        {
            ProcessingCompleteHandle.Reset();

            State = PodState.Running;

            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
            {
                _logger.Verbose("delay for message {id} is {msec}", msg.Id, msg.ProcessingMsec);
            }

            _message = new MessageOut()
            {
                Text = $"Your new outbound message is: {msg.Text} brought to you from pod {this.Id.ToString()}",
                Id = msg.Id + 10000,
                MessageInId = msg.Id,
                RuntimeMsec = msg.ProcessingMsec,
                ProcessedByPod = Idx
            };

            Thread.Sleep(msg.ProcessingMsec);

            State = PodState.ReadyToUnload;

            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                _logger.Debug("Pod {idx} is complete {state}", Idx, State);
            }

            ProcessingCompleteHandle.Set();
        }

        //
        public int GetMessageId()
        {
            return this._message.MessageInId;
        }

        // TODO come back to this
        // what I actually want to return here is a copy of the message and
        // then set internal copy to null
        public MessageOut? Unload()
        {
            State = PodState.WaitingToLoad;
            return this._message;
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
