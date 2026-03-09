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

    public class Pod<TMessageIn, TMessageOut> : IProcessingContainer<TMessageIn, TMessageOut> 
    {
        readonly ILogger _logger = Log.ForContext<Pod<TMessageIn, TMessageOut>>();

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
        private TMessageOut _message;

        /// <summary>
        /// Optional translation function. If set, this will be used to convert an inbound
        /// message to the outbound message type. Signature: TMessageOut Go(TMessageIn)
        /// </summary>
        public Func<TMessageIn, TMessageOut>? Go { get; set; }

        private EventWaitHandle ProcessingCompleteHandle = new(true, EventResetMode.ManualReset);

        public Pod(int idx)
        {
            Idx = idx;
            State = PodState.WaitingToLoad;
        }

        //
        public void ProcessMessage(TMessageIn msg)
        {
            if (State != PodState.WaitingToLoad)
            {
                throw new InvalidOperationException($"Pod {Idx} is in State {State}");
            }

            State = PodState.Loading;

            Thread ProcessingThread = new(() => ProcessThreadFunc(msg));
            ProcessingThread.Start();
        }

        internal TMessageOut InternalProcess(TMessageIn msg)
        {
            if (Go != null)
            {
                return Go(msg);
            }
            throw new InvalidOperationException("No converter provided: set the Pod.Go property to convert TMessageIn to TMessageOut.");
        }


        internal void ProcessThreadFunc(TMessageIn msg)
        {

            ProcessingCompleteHandle.Reset();

            LastExecutionTime = TimeSpan.Zero;

            Stopwatch sw = Stopwatch.StartNew();

            State = PodState.Running;

            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                _logger.Debug("ProcessThreadFunc in Pod {idx} has started", Idx);
            }

            _message = InternalProcess(msg);

            State = PodState.ReadyToUnload;

            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                _logger.Debug("Pod {idx:D3} ProcessThreadFunc has Completed {state}", Idx, State);
            }

            sw.Stop();
            LastExecutionTime = sw.Elapsed;

            ProcessingCompleteHandle.Set();
        }


        /// <summary>
        /// Unlpoad the processed message from the pod.
        /// </summary>
        /// <returns></returns>
        public TMessageOut? Unload()
        {
            if (_message == null)
            {
                _logger.Warning("Pod {idx} has no message to unload", Idx);
                State = PodState.WaitingToLoad;
                return default(TMessageOut);
            }

            TMessageOut ret = _message;//.Clone();
            //_message = null;

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
