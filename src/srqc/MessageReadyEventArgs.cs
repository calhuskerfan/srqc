namespace Srqc
{
    public class MessageReadyEventArgs<TMessageOut> : EventArgs
    {
        public required TMessageOut Message { get; set; }
        public int RuntimeMsec { get; set; }
        public Guid ProcessedByPod { get; set; }
        public int ProcessedByPodIdx { get; set; }
    }
}
