namespace AiDebugger.Packager;

public class EvidenceBundle
{
    public string? Service { get; set; }
    public string? Env { get; set; }
    public string? Version { get; set; }
    public Dictionary<string, int> ExceptionCounts { get; set; } = new();
    public List<string> SampleStacktraces { get; set; } = new();
    public List<ChainEntry> Chains { get; set; } = new();
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
}

public class ChainEntry
{
    public List<string> Sequence { get; set; } = new();
    public int Count { get; set; }
}
