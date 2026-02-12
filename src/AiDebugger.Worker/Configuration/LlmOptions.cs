namespace AiDebugger.Worker.Configuration;

public class LlmOptions
{
    public const string Section = "LLM";
    public string Provider { get; set; } = "OpenAI";
    public string Model { get; set; } = "gpt-4o-mini";
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
}
