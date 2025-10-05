namespace srqc.domain
{
    public class ConduitConfig : IConduitConfig
    {
        public int PodCount { get; set; }
        public bool ReUsePods { get; set; } = false;
    }
}
