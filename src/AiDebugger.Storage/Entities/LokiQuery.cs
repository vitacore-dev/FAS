namespace AiDebugger.Storage.Entities;

public class LokiQuery
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Logql { get; set; } = null!;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
