namespace srqc.domain
{
    public interface IProcessingContainer
    {
        event EventHandler<MessageReadyEventArgs>? MessageReadyAtExitEvent;
        bool IsConduitEmpty();
        void LoadMessage(IClaimCheck ticket, MessageIn message);
        void Stop();
        IClaimCheck WaitForStagingQueueSlotAvailable();
    }

    public interface IClaimCheck
    {
        Guid Ticket { get; set; }
        DateTime Issued { get; }
    }
}
