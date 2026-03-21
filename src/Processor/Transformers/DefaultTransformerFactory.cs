using Srqc;
using Srqc.Domain;

namespace Processor.Transformers
{
    public class DefaultTransformerFactory : ITransformerFactory<MessageIn, MessageOut>
    {
        public Func<MessageIn, MessageOut> GetTransformer()
        {
            return (MessageIn m) =>
            {
                Thread.Sleep(m.ProcessingMsec);

                return new MessageOut
                {
                    Text = $"New outbound message is: {m.Text}",
                    Id = m.Id + 10000,
                    MessageInId = m.Id,
                };
            };
        }
    }
}
