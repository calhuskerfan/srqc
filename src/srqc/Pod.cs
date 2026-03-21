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

    /// <summary>
    /// Represents a processing container that handles messages of type TMessageIn and converts them to type TMessageOut
    /// using an optional translation function.
    /// </summary>
    /// <remarks>The Pod class manages its state throughout the message processing lifecycle, including
    /// loading, running, and unloading states. It provides a mechanism to wait for processing completion and allows for
    /// optional message translation via the Go property.</remarks>
    /// <typeparam name="TMessageIn">The type of the inbound message that the Pod processes.</typeparam>
    /// <typeparam name="TMessageOut">The type of the outbound message that the Pod produces after processing the inbound message.</typeparam>
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
        private TMessageOut? _message;

        /// <summary>
        /// Optional translation function. If set, this will be used to convert an inbound
        /// message to the outbound message type. Signature: TMessageOut Go(TMessageIn)
        /// </summary>
        private Func<TMessageIn, TMessageOut> _transformer;

        private EventWaitHandle ProcessingCompleteHandle = new(true, EventResetMode.ManualReset);

        public Pod(int idx, Func<TMessageIn, TMessageOut> transformer)
        {
            Idx = idx;
            _transformer = transformer;
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
            return _transformer(msg);
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
        /// <remarks>
        /// This is the one section that I am not sure about.
        /// Should this be a method that returns the message as it is now or could we set a property that Unload() would read from
        /// The issue that makes me nervous is if someone holds a reference to the message and then we set it to null or default,
        /// they would have a reference to the message but it would be in an undefined state.
        /// </remarks>
        /// <returns></returns>
        public TMessageOut? Unload()
        {
            if (_message == null)
            {
                _logger.Warning("Pod {idx} has no message to unload", Idx);
                State = PodState.WaitingToLoad;
                //NOTE: Should this be a default(TMessageOut) or should it be nullable and return null?
                return default(TMessageOut);
            }

            TMessageOut ret = _message;

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
