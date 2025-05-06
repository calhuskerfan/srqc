namespace srqc.domain
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
    }
}
