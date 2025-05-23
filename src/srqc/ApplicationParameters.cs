
using srqc.domain;

namespace srqc
{
    public class ApplicationParameters
    {
        public int PodCount { get; set; } = 3;
        public int MessageCount { get; set; } = 10;
        public int MinProcessingDelay { get; set; } = 100;
        public int MaxProcessingDelay { get; set; } = 300;
        public bool LogMessageReadyInvokes { get; set; } = false;
        public bool SuppressNoisyINF { get; set; } = false;
    }
}