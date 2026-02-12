using AiDebugger.Storage;
using AiDebugger.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiDebugger.Ingest;

public class LokiIngestService
{
    private readonly LokiClient _loki;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly int _watermarkSeconds;

    public LokiIngestService(
        LokiClient loki,
        IDbContextFactory<AppDbContext> dbFactory,
        int watermarkSeconds = 120)
    {
        _loki = loki;
        _dbFactory = dbFactory;
        _watermarkSeconds = watermarkSeconds;
    }

    public async Task<IngestResult> IngestAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var queries = await db.LokiQueries.Where(q => q.Enabled).ToListAsync(ct).ConfigureAwait(false);
        var totalEvents = 0;
        foreach (var q in queries)
        {
            var (from, checkpoint) = await GetWindowAsync(db, q.Id, ct).ConfigureAwait(false);
            var to = DateTime.UtcNow.AddSeconds(-_watermarkSeconds);
            if (from >= to) continue;

            var response = await _loki.QueryRangeAsync(q.Logql, from, to, 5000, ct).ConfigureAwait(false);
            var events = ParseStreams(response, q.Id);
            if (events.Count == 0)
            {
                await UpdateCheckpointAsync(db, q.Id, to, _watermarkSeconds, ct).ConfigureAwait(false);
                continue;
            }

            await db.RawEvents.AddRangeAsync(events, ct).ConfigureAwait(false);
            totalEvents += events.Count;
            await UpdateCheckpointAsync(db, q.Id, to, _watermarkSeconds, ct).ConfigureAwait(false);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return new IngestResult { EventsIngested = totalEvents };
    }

    private static async Task<(DateTime from, Checkpoint?)> GetWindowAsync(AppDbContext db, string queryId, CancellationToken ct)
    {
        var cp = await db.Checkpoints.FindAsync(new object[] { queryId }, ct).ConfigureAwait(false);
        if (cp != null)
            return (cp.LastProcessedTs, cp);
        var from = DateTime.UtcNow.AddHours(-1);
        return (from, null);
    }

    private static List<RawEvent> ParseStreams(LokiQueryRangeResponse response, string sourceQueryId)
    {
        var list = new List<RawEvent>();
        if (response.Data?.Result == null) return list;
        foreach (var stream in response.Data.Result)
        {
            var labels = stream.Stream ?? new Dictionary<string, string>();
            foreach (var pair in stream.Values)
            {
                if (pair == null || pair.Count < 2) continue;
                var tsStr = pair[0];
                var line = pair[1] ?? "";
                if (string.IsNullOrEmpty(tsStr)) continue;
                if (!long.TryParse(tsStr, out var ns))
                    continue;
                var ts = DateTimeOffset.FromUnixTimeMilliseconds(ns / 1_000_000).UtcDateTime;
                labels.TryGetValue("service", out var service);
                labels.TryGetValue("job", out var job);
                if (string.IsNullOrEmpty(service)) service = job;
                var evt = new RawEvent
                {
                    Id = Guid.NewGuid(),
                    Ts = ts,
                    Service = service ?? sourceQueryId,
                    Message = line,
                    RawJson = System.Text.Json.JsonSerializer.Serialize(labels)
                };
                list.Add(evt);
            }
        }
        return list;
    }

    private static async Task UpdateCheckpointAsync(AppDbContext db, string queryId, DateTime lastTs, int watermarkSeconds, CancellationToken ct)
    {
        var cp = await db.Checkpoints.FindAsync(new object[] { queryId }, ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        if (cp == null)
        {
            db.Checkpoints.Add(new Checkpoint
            {
                QueryId = queryId,
                LastProcessedTs = lastTs,
                WatermarkSeconds = watermarkSeconds,
                UpdatedAt = now
            });
        }
        else
        {
            cp.LastProcessedTs = lastTs;
            cp.UpdatedAt = now;
        }
    }
}

public class IngestResult
{
    public int EventsIngested { get; set; }
}
