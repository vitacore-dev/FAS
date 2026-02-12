namespace AiDebugger.Storage.Entities;

public class Fingerprint
{
    public string FingerprintId { get; set; } = null!;
    public string ExceptionType { get; set; } = null!;
    public string? TopFramesJson { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public string? LastSeenService { get; set; }
    public string? LastSeenEnv { get; set; }
    public string? LastSeenVersion { get; set; }
    public string Status { get; set; } = "new"; // new | known | fixed | ignored
    public string? OwnerTeam { get; set; }
}
