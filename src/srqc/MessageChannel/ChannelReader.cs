namespace Srqc.MessageChannel
{
    public class ChannelReader: ChannelBase, IChannelReader
    {
        public ushort PrefetchCount { get; set; } = 3;
    }
}
