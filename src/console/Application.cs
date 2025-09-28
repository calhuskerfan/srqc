using srqc;
using srqc.domain;

namespace console
{
    public class Application
    {
        internal static Random r = new();

        public static IProcessingSystem GetProcessingContainer(ApplicationParameters parameters)
        {
            return new Conduit(config: new ConduitConfig()
            {
                PodCount = parameters.PodCount,
                ReUsePods = parameters.ReUsePods,
            });
        }

        /// <summary>
        /// Load Inbound Messages Builds a set of test messages.
        /// </summary>
        /// <remarks>
        /// Case 0: the default, based on appParams
        /// Case 1: demonstrates a simple example where pod 1 finishes and starts message 4 while pods 2 and 3 are still running.
        /// Case 1: demonstrates improvement possibilities by processing all five messages in little more than the 'longest' pole at message 3
        /// </remarks>
        /// <param name="inboundMessages"></param>
        /// <param name="appParams"></param>
        /// <param name="testcase"></param>
        public static void LoadInboundMessages(
            ref List<MessageIn> inboundMessages,
            ref ApplicationParameters appParams,
            int testcase = 0)
        {
            switch (testcase)
            {
                case 1:
                    inboundMessages.Add(new MessageIn() { Id = 1, Text = "1", ProcessingMsec = 100 });
                    inboundMessages.Add(new MessageIn() { Id = 2, Text = "2", ProcessingMsec = 500 });
                    inboundMessages.Add(new MessageIn() { Id = 3, Text = "3", ProcessingMsec = 1000 });
                    inboundMessages.Add(new MessageIn() { Id = 4, Text = "4", ProcessingMsec = 100 });
                    appParams.PodCount = 3;
                    break;
                case 2:
                    inboundMessages.Add(new MessageIn() { Id = 1, Text = "1", ProcessingMsec = 100 });
                    inboundMessages.Add(new MessageIn() { Id = 2, Text = "2", ProcessingMsec = 700 });
                    inboundMessages.Add(new MessageIn() { Id = 3, Text = "3", ProcessingMsec = 1000 });
                    inboundMessages.Add(new MessageIn() { Id = 4, Text = "4", ProcessingMsec = 900 });
                    inboundMessages.Add(new MessageIn() { Id = 5, Text = "5", ProcessingMsec = 100 });
                    appParams.PodCount = 3;
                    break;
                default:
                    {
                        for (int i = 1; i < appParams.MessageCount + 1; i++)
                        {
                            inboundMessages.Add(new MessageIn()
                            {
                                Id = i,
                                Text = i.ToString(),
                                ProcessingMsec = r.Next(appParams.MinProcessingDelay, appParams.MaxProcessingDelay)
                            });
                        }
                    }
                    break;
            }
        }
    }
}
