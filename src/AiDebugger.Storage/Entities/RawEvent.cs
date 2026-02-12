namespace AiDebugger.Storage.Entities;

public class RawEvent
{
    public Guid Id { get; set; }
    public DateTime Ts { get; set; }
    public string? Service { get; set; }
    public string? Env { get; set; }
    public string? Version { get; set; }
    public string? Host { get; set; }
    public string? Level { get; set; }
    public string? TraceId { get; set; }
    public string? SessionId { get; set; }
    public string? ClientId { get; set; }
    public string? ThreadId { get; set; }
    public string? FingerprintId { get; set; }
    public string? ExceptionType { get; set; }
    public string? Message { get; set; }
    public string? Stacktrace { get; set; }
    public string? RawJson { get; set; }
}
