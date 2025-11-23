using Microsoft.Extensions.Configuration;

namespace Srqc
{
    public class ConduitConfig : IConduitConfig
    {
        public int PodCount { get; set; }
        public bool ReUsePods { get; set; } = false;
    }
}
