using AiDebugger.Storage;
using AiDebugger.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AiDebugger.Packager;

public class PackagerService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PackagerService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<EvidenceBundle> BuildEvidenceBundleAsync(
        DateTime from,
        DateTime to,
        int maxSamples = 5,
        int maxTokensHint = 15000,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var events = await db.RawEvents
            .Where(e => e.Ts >= from && e.Ts <= to)
            .OrderBy(e => e.Ts)
            .Take(2000)
            .ToListAsync(ct).ConfigureAwait(false);

        var exceptionCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var fingerprintSamples = new Dictionary<string, List<string>>();
        var chains = new List<ChainEntry>();
        string? service = null, env = null, version = null;

        foreach (var e in events)
        {
            if (!string.IsNullOrEmpty(e.Service)) service = e.Service;
            if (!string.IsNullOrEmpty(e.Env)) env = e.Env;
            if (!string.IsNullOrEmpty(e.Version)) version = e.Version;

            var (exType, frames) = FingerprintBuilder.ParseStacktrace(e.Message);
            if (string.IsNullOrEmpty(exType)) continue;

            exceptionCounts[exType] = exceptionCounts.GetValueOrDefault(exType) + 1;
            var fp = FingerprintBuilder.ComputeFingerprint(exType, frames);
            if (!fingerprintSamples.ContainsKey(fp))
                fingerprintSamples[fp] = new List<string>();
            if (fingerprintSamples[fp].Count < maxSamples && !string.IsNullOrEmpty(e.Message))
                fingerprintSamples[fp].Add(e.Message.Trim().Length > 2000 ? e.Message.Trim()[..2000] + "..." : e.Message.Trim());
        }

        var samples = fingerprintSamples.Values.SelectMany(x => x).Distinct().Take(maxSamples).ToList();
        var bundle = new EvidenceBundle
        {
            Service = service,
            Env = env,
            Version = version,
            ExceptionCounts = exceptionCounts,
            SampleStacktraces = samples,
            Chains = chains,
            WindowStart = from,
            WindowEnd = to
        };
        return bundle;
    }

    public async Task UpdateFingerprintsFromEventsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var events = await db.RawEvents
            .Where(e => e.Ts >= from && e.Ts <= to && e.Message != null)
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var e in events)
        {
            var (exType, frames) = FingerprintBuilder.ParseStacktrace(e.Message);
            if (string.IsNullOrEmpty(exType) || frames.Count == 0) continue;

            var fpId = FingerprintBuilder.ComputeFingerprint(exType, frames);
            var existing = await db.Fingerprints.FindAsync(new object[] { fpId }, ct).ConfigureAwait(false);
            if (existing == null)
            {
                db.Fingerprints.Add(new Fingerprint
                {
                    FingerprintId = fpId,
                    ExceptionType = exType,
                    TopFramesJson = JsonSerializer.Serialize(frames.Take(5).ToList()),
                    FirstSeenAt = e.Ts,
                    LastSeenAt = e.Ts,
                    LastSeenService = e.Service,
                    LastSeenEnv = e.Env,
                    LastSeenVersion = e.Version,
                    Status = "new"
                });
            }
            else
            {
                existing.LastSeenAt = e.Ts;
                existing.LastSeenService = e.Service;
                existing.LastSeenEnv = e.Env;
                existing.LastSeenVersion = e.Version;
            }
            e.FingerprintId = fpId;
            e.ExceptionType = exType;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
