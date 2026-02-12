using AiDebugger.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiDebugger.Storage;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<LokiQuery> LokiQueries => Set<LokiQuery>();
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
    public DbSet<Fingerprint> Fingerprints => Set<Fingerprint>();
    public DbSet<RawEvent> RawEvents => Set<RawEvent>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<IntegrationIssue> IntegrationIssues => Set<IntegrationIssue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LokiQuery>(e =>
        {
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<Checkpoint>(e =>
        {
            e.HasKey(x => x.QueryId);
        });

        modelBuilder.Entity<Fingerprint>(e =>
        {
            e.HasKey(x => x.FingerprintId);
            e.HasIndex(x => new { x.ExceptionType, x.LastSeenAt });
            e.HasIndex(x => new { x.Status, x.LastSeenAt });
        });

        modelBuilder.Entity<RawEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Ts);
            e.HasIndex(x => new { x.FingerprintId, x.Ts });
        });

        modelBuilder.Entity<Finding>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.FingerprintId, x.CreatedAt });
            e.HasIndex(x => new { x.Status, x.Severity, x.UpdatedAt });
        });

        modelBuilder.Entity<IntegrationIssue>(e =>
        {
            e.HasKey(x => new { x.Provider, x.FingerprintId });
        });
    }
}
