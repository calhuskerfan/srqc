namespace srqc.domain
{
    public interface IProcessingContainer
    {
        event EventHandler<MessageReadyEventArgs>? MessageReadyAtExitEvent;
        bool IsContainerEmpty();
        void LoadMessage(MessageIn message);
        void Stop();
        void WaitForStagingQueue();
    }
}
