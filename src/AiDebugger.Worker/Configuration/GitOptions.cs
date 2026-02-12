namespace AiDebugger.Worker.Configuration;

public class GitOptions
{
    public const string Section = "Git";
    public string RepoPath { get; set; } = "";
    public string Branch { get; set; } = "main";
}
