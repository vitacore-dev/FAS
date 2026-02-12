namespace AiDebugger.Worker.Configuration;

public class WorkerOptions
{
    public const string Section = "Worker";
    public int IngestIntervalMinutes { get; set; } = 5;
    public int WatermarkSeconds { get; set; } = 120;
}
