namespace AiDebugger.Worker.Configuration;

public class LokiOptions
{
    public const string Section = "Loki";
    public string Url { get; set; } = "http://localhost:3100";
    public string? ApiKey { get; set; }
}
