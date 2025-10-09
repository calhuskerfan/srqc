using Microsoft.Extensions.Configuration;

namespace srqc.domain
{
    public class ConduitConfig
    {
        public int PodCount { get; set; }
        public bool ReUsePods { get; set; } = false;
    }
}
