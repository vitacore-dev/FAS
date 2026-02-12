namespace AiDebugger.Orchestrator;

public class OrchestratorResult
{
    public string FingerprintId { get; set; } = "";
    public string Severity { get; set; } = "medium";
    public decimal PriorityScore { get; set; }
    public string? RootCauseChainJson { get; set; }
    public string? LogEvidenceJson { get; set; }
    public string? CodeEvidenceJson { get; set; }
    public string? SuggestedFixJson { get; set; }
    public string? LlmModel { get; set; }
    public int? LatencyMs { get; set; }
}
