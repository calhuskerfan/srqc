using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srqc.MessageChannel
{
    public class ChannelReader: ChannelBase
    {
        //_prefetchCount
        public ushort PrefetchCount { get; set; } = 3;
    }
}
