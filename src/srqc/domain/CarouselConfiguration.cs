namespace srqc.domain
{
    public class CarouselConfiguration
    {
        public int PodCount { get; set; }
        public int BoardingQueueSize { get; set; } = 3;
        public bool LogInvoke { get; set; } = false;
        public bool SuppressNoisyINF { get; set; } = false;
    }
}
