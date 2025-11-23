namespace Processor
{
    public interface IWorkerContext
    {
        int MaxProcessingDelay { get; set; }
        int MinProcessingDelay { get; set; }
    }
}