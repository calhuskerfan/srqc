namespace Srqc
{
    public interface IProcessingSystem<TMessageIn, TMessageOut>
    {
        event EventHandler<MessageReadyEventArgs<TMessageOut>>? MessageReadyAtExitEvent;
        bool IsSystemEmpty();
        void LoadMessage(IClaimCheck ticket, TMessageIn message);
        void Stop();
        IClaimCheck WaitForProcessingSlotAvailable();
    }

    public interface ITransformer<TMessageIn, TMessageOut>
    {
        TMessageOut Transform(TMessageIn msg);
    }


    public interface ITransformerFactory<TMessageIn, TMessageOut>
    {
        Func<TMessageIn, TMessageOut> GetTransformer();
    }

    public interface IProcessingContainer<TMessageIn, TMessageOut>
    {
        void ProcessMessage(TMessageIn msg);
        Guid Id { get; }
    }

    public interface IConduitConfig
    {
        int PodCount { get; set; }
        bool ReUsePods { get; set; }
    }

    public interface IClaimCheck
    {
        Guid Ticket { get;}
        DateTime Issued { get; }
    }
}
