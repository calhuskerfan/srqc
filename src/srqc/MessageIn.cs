namespace Srqc
{
    public class MessageIn
    {
        public int Id { get; set; }
        public required string Text { get; set; }
        public int ProcessingMsec { get; set; }

        public override string ToString()
        {
            return $"{Id}. {Text}";
        }
    }
}
