using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiDebugger.Storage.Migrations;

// Apply with: dotnet ef database update --project src/AiDebugger.Storage --startup-project src/AiDebugger.Worker

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LokiQueries",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Logql = table.Column<string>(type: "text", nullable: false),
                Enabled = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_LokiQueries", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Checkpoints",
            columns: table => new
            {
                QueryId = table.Column<string>(type: "text", nullable: false),
                LastProcessedTs = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                WatermarkSeconds = table.Column<int>(type: "integer", nullable: false),
                LastOffset = table.Column<string>(type: "text", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Checkpoints", x => x.QueryId));

        migrationBuilder.CreateTable(
            name: "Fingerprints",
            columns: table => new
            {
                FingerprintId = table.Column<string>(type: "text", nullable: false),
                ExceptionType = table.Column<string>(type: "text", nullable: false),
                TopFramesJson = table.Column<string>(type: "text", nullable: true),
                FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastSeenService = table.Column<string>(type: "text", nullable: true),
                LastSeenEnv = table.Column<string>(type: "text", nullable: true),
                LastSeenVersion = table.Column<string>(type: "text", nullable: true),
                Status = table.Column<string>(type: "text", nullable: false),
                OwnerTeam = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Fingerprints", x => x.FingerprintId));

        migrationBuilder.CreateIndex(
            name: "IX_Fingerprints_ExceptionType_LastSeenAt",
            table: "Fingerprints",
            columns: new[] { "ExceptionType", "LastSeenAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Fingerprints_Status_LastSeenAt",
            table: "Fingerprints",
            columns: new[] { "Status", "LastSeenAt" });

        migrationBuilder.CreateTable(
            name: "RawEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Service = table.Column<string>(type: "text", nullable: true),
                Env = table.Column<string>(type: "text", nullable: true),
                Version = table.Column<string>(type: "text", nullable: true),
                Host = table.Column<string>(type: "text", nullable: true),
                Level = table.Column<string>(type: "text", nullable: true),
                TraceId = table.Column<string>(type: "text", nullable: true),
                SessionId = table.Column<string>(type: "text", nullable: true),
                ClientId = table.Column<string>(type: "text", nullable: true),
                ThreadId = table.Column<string>(type: "text", nullable: true),
                FingerprintId = table.Column<string>(type: "text", nullable: true),
                ExceptionType = table.Column<string>(type: "text", nullable: true),
                Message = table.Column<string>(type: "text", nullable: true),
                Stacktrace = table.Column<string>(type: "text", nullable: true),
                RawJson = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_RawEvents", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_RawEvents_Ts",
            table: "RawEvents",
            column: "Ts");

        migrationBuilder.CreateIndex(
            name: "IX_RawEvents_FingerprintId_Ts",
            table: "RawEvents",
            columns: new[] { "FingerprintId", "Ts" });

        migrationBuilder.CreateTable(
            name: "Findings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FingerprintId = table.Column<string>(type: "text", nullable: false),
                Service = table.Column<string>(type: "text", nullable: true),
                Env = table.Column<string>(type: "text", nullable: true),
                VersionRange = table.Column<string>(type: "text", nullable: true),
                Severity = table.Column<string>(type: "text", nullable: false),
                PriorityScore = table.Column<decimal>(type: "numeric", nullable: false),
                RootCauseChainJson = table.Column<string>(type: "text", nullable: true),
                LogEvidenceJson = table.Column<string>(type: "text", nullable: true),
                CodeEvidenceJson = table.Column<string>(type: "text", nullable: true),
                SuggestedFixJson = table.Column<string>(type: "text", nullable: true),
                Status = table.Column<string>(type: "text", nullable: false),
                PromptVersion = table.Column<string>(type: "text", nullable: true),
                LlmModel = table.Column<string>(type: "text", nullable: true),
                LatencyMs = table.Column<int>(type: "integer", nullable: true),
                FeedbackScore = table.Column<decimal>(type: "numeric", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Findings", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Findings_FingerprintId_CreatedAt",
            table: "Findings",
            columns: new[] { "FingerprintId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Findings_Status_Severity_UpdatedAt",
            table: "Findings",
            columns: new[] { "Status", "Severity", "UpdatedAt" });

        migrationBuilder.CreateTable(
            name: "IntegrationIssues",
            columns: table => new
            {
                Provider = table.Column<string>(type: "text", nullable: false),
                FingerprintId = table.Column<string>(type: "text", nullable: false),
                IssueKey = table.Column<string>(type: "text", nullable: false),
                Url = table.Column<string>(type: "text", nullable: true),
                State = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_IntegrationIssues", x => new { x.Provider, x.FingerprintId }));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LokiQueries");
        migrationBuilder.DropTable(name: "Checkpoints");
        migrationBuilder.DropTable(name: "Fingerprints");
        migrationBuilder.DropTable(name: "RawEvents");
        migrationBuilder.DropTable(name: "Findings");
        migrationBuilder.DropTable(name: "IntegrationIssues");
    }
}
