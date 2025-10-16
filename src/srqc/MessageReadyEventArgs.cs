namespace Srqc
{
    public class MessageReadyEventArgs : EventArgs
    {
        public required MessageOut Message { get; set; }
    }
}
