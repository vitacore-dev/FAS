namespace AiDebugger.Storage.Entities;

public class IntegrationIssue
{
    public string Provider { get; set; } = null!; // gitlab | github | jira
    public string FingerprintId { get; set; } = null!;
    public string IssueKey { get; set; } = null!;
    public string? Url { get; set; }
    public string State { get; set; } = "open"; // open | closed
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
