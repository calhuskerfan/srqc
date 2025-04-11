
using srqc.domain;

namespace srqc
{
    public class ApplicationParameters
    {
        public int PodCount { get; set; } = 3;
        public int BatchCount { get; set; } = 1;
        public int MessageCount { get; set; } = 10;
        public int MinProcessingDelay { get; set; } = 750;
        public int MaxProcessingDelay { get; set; } = 1450;
    }
}