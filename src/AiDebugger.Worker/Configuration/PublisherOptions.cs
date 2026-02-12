namespace AiDebugger.Worker.Configuration;

public class PublisherOptions
{
    public const string Section = "Publisher";
    public string Provider { get; set; } = "GitHub";
    public string RepoOwner { get; set; } = "";
    public string RepoName { get; set; } = "";
    public string? Token { get; set; }
    public List<string> Labels { get; set; } = new() { "ai-debugger" };
}
