using srqc;
using srqc.domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace console
{
    public class Application
    {
        internal static Random r = new();

        public static IProcessingContainer GetProcessingContainer(ApplicationParameters parameters)
        {
            return new Conduit(config: new ConduitConfig()
            {
                PodCount = parameters.PodCount,
                ReUsePods = parameters.ReUsePods,
            });
        }

        public static void LoadInboundMessages(
            ref List<MessageIn> inboundMessages,
            ApplicationParameters appParams,
            int testcase = 0)
        {
            switch (testcase)
            {
                case 1:
                    inboundMessages.Add(new MessageIn() { Id = 1, Text = "1", ProcessingMsec = 100 });
                    inboundMessages.Add(new MessageIn() { Id = 2, Text = "2", ProcessingMsec = 500 });
                    inboundMessages.Add(new MessageIn() { Id = 3, Text = "3", ProcessingMsec = 1000 });
                    inboundMessages.Add(new MessageIn() { Id = 4, Text = "4", ProcessingMsec = 100 });
                    break;
                case 2:
                    inboundMessages.Add(new MessageIn() { Id = 1, Text = "1", ProcessingMsec = 100 });
                    inboundMessages.Add(new MessageIn() { Id = 2, Text = "2", ProcessingMsec = 700 });
                    inboundMessages.Add(new MessageIn() { Id = 3, Text = "3", ProcessingMsec = 1000 });
                    inboundMessages.Add(new MessageIn() { Id = 4, Text = "4", ProcessingMsec = 900 });
                    inboundMessages.Add(new MessageIn() { Id = 5, Text = "5", ProcessingMsec = 100 });
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
