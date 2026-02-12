namespace AiDebugger.Storage.Entities;

public class Checkpoint
{
    public string QueryId { get; set; } = null!;
    public DateTime LastProcessedTs { get; set; }
    public int WatermarkSeconds { get; set; } = 120;
    public string? LastOffset { get; set; }
    public DateTime UpdatedAt { get; set; }
}
