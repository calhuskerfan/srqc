namespace Srqc
{
    public class MessageOut
    {
        public int Id { get; set; }
        public int MessageInId { get; set; }
        public required string Text { get; set; }
        public int RuntimeMsec { get; set; }
        public Guid ProcessedByPod { get; set; }
        public int ProcessedByPodIdx { get; set; }


        public override string ToString()
        {
            return $"MessageOut:{Id:D5}:{MessageInId:D5}:{Text}";
        }

        /// <summary>
        /// Creates a shallow copy of the current MessageOut instance.
        /// This is ok for now, but if we add reference type properties in the future,
        /// we want to keep this in mind.
        /// </summary>
        /// <returns></returns>
        public MessageOut Clone()
        {
            return (MessageOut)this.MemberwiseClone();
        }
    }
}
