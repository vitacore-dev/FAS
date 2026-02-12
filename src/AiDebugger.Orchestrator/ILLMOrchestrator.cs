using AiDebugger.Packager;

namespace AiDebugger.Orchestrator;

public interface ILLMOrchestrator
{
    Task<OrchestratorResult?> AnalyzeAsync(EvidenceBundle bundle, string fingerprintId, CancellationToken ct = default);
}
