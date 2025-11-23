namespace console
{
    public class ApplicationParameters
    {
        public int MessageCount { get; set; } = 10;
        public int MinProcessingDelay { get; set; } = 100;
        public int MaxProcessingDelay { get; set; } = 300;
        public int TestScenario { get; set; } = 0;
    }
}