namespace AiDebugger.Storage.Entities;

public class Finding
{
    public Guid Id { get; set; }
    public string FingerprintId { get; set; } = null!;
    public string? Service { get; set; }
    public string? Env { get; set; }
    public string? VersionRange { get; set; }
    public string Severity { get; set; } = "medium"; // low | medium | high | critical
    public decimal PriorityScore { get; set; }
    public string? RootCauseChainJson { get; set; }
    public string? LogEvidenceJson { get; set; }
    public string? CodeEvidenceJson { get; set; }
    public string? SuggestedFixJson { get; set; }
    public string Status { get; set; } = "new"; // new | triaged | in_progress | fixed | false_positive
    public string? PromptVersion { get; set; }
    public string? LlmModel { get; set; }
    public int? LatencyMs { get; set; }
    public decimal? FeedbackScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
