namespace Processor
{
    public class WorkerContext : IWorkerContext
    {
        public int MinProcessingDelay { get; set; } = 100;
        public int MaxProcessingDelay { get; set; } = 300;

        public WorkerContext(IConfiguration configuration)
        {
            MinProcessingDelay = Convert.ToInt32(configuration["AppSettings:MinProcessingDelay"]);
            MaxProcessingDelay = Convert.ToInt32(configuration["AppSettings:MaxProcessingDelay"]);
        }
    }
}
