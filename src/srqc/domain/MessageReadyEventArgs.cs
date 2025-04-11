namespace srqc.domain
{
    public class MessageReadyEventArgs : EventArgs
    {
        public MessageOut? Message { get; set; }
    }
}
