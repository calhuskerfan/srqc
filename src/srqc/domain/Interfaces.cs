namespace srqc.domain
{
    public interface IProcessingSystem
    {
        event EventHandler<MessageReadyEventArgs>? MessageReadyAtExitEvent;
        bool IsSystemEmpty();
        void LoadMessage(IClaimCheck ticket, MessageIn message);
        void Stop();
        IClaimCheck WaitForProcessingSlotAvailable();
    }

    public interface IClaimCheck
    {
        Guid Ticket { get; set; }
        DateTime Issued { get; }
    }
}
