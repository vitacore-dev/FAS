using AiDebugger.Ingest;
using AiDebugger.Orchestrator;
using AiDebugger.Packager;
using AiDebugger.Publisher;
using AiDebugger.Storage;
using AiDebugger.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiDebugger.Worker;

public class PipelineService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly LokiIngestService _ingest;
    private readonly PackagerService _packager;
    private readonly ILLMOrchestrator _orchestrator;
    private readonly IPublisher _publisher;
    private readonly ILogger<PipelineService> _logger;

    public PipelineService(
        IDbContextFactory<AppDbContext> dbFactory,
        LokiIngestService ingest,
        PackagerService packager,
        ILLMOrchestrator orchestrator,
        IPublisher publisher,
        ILogger<PipelineService> logger)
    {
        _dbFactory = dbFactory;
        _ingest = ingest;
        _packager = packager;
        _orchestrator = orchestrator;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Pipeline: starting ingest");
        var ingestResult = await _ingest.IngestAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Pipeline: ingested {Count} events", ingestResult.EventsIngested);

        var to = DateTime.UtcNow;
        var from = to.AddHours(-1);
        await _packager.UpdateFingerprintsFromEventsAsync(from, to, ct).ConfigureAwait(false);

        var bundle = await _packager.BuildEvidenceBundleAsync(from, to, 5, 15000, ct).ConfigureAwait(false);
        if (bundle.ExceptionCounts.Count == 0)
        {
            _logger.LogInformation("Pipeline: no exceptions in window, skip analysis");
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var newFingerprints = await db.Fingerprints
            .Where(f => f.Status == "new" && f.LastSeenAt >= from)
            .Take(5)
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var fp in newFingerprints)
        {
            ct.ThrowIfCancellationRequested();
            var result = await _orchestrator.AnalyzeAsync(bundle, fp.FingerprintId, ct).ConfigureAwait(false);
            if (result == null) continue;

            var finding = new Finding
            {
                Id = Guid.NewGuid(),
                FingerprintId = result.FingerprintId,
                Service = bundle.Service,
                Env = bundle.Env,
                Severity = result.Severity,
                PriorityScore = result.PriorityScore,
                RootCauseChainJson = result.RootCauseChainJson,
                LogEvidenceJson = result.LogEvidenceJson,
                CodeEvidenceJson = result.CodeEvidenceJson,
                SuggestedFixJson = result.SuggestedFixJson,
                Status = "new",
                LlmModel = result.LlmModel,
                LatencyMs = result.LatencyMs,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Findings.Add(finding);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            var existingIssue = await db.IntegrationIssues
                .FirstOrDefaultAsync(i => i.Provider == "GitHub" && i.FingerprintId == fp.FingerprintId && i.State == "open", ct).ConfigureAwait(false);
            if (existingIssue != null)
            {
                _logger.LogInformation("Pipeline: issue already exists for {Fingerprint}", fp.FingerprintId);
                fp.Status = "known";
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                continue;
            }

            var publishResult = await _publisher.PublishAsync(finding, fp, ct).ConfigureAwait(false);
            if (publishResult.Created)
            {
                db.IntegrationIssues.Add(new IntegrationIssue
                {
                    Provider = "GitHub",
                    FingerprintId = fp.FingerprintId,
                    IssueKey = publishResult.IssueKey!,
                    Url = publishResult.Url,
                    State = "open",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                fp.Status = "known";
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("Pipeline: created issue {Key} for {Fingerprint}", publishResult.IssueKey, fp.FingerprintId);
            }
        }
    }
}
