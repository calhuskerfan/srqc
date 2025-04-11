namespace srqc.domain
{
    public class MessageOut
    {
        public int Id { get; set; }
        public int MessageInId { get; set; }
        public required string Text { get; set; }
        public int RuntimeMsec { get; set; }
        public int ProcessedByPod { get; set; }


        public override string ToString()
        {
            return $"{Id}. {MessageInId}. {Text}";
        }
    }
}
