using AiDebugger.Storage.Entities;

namespace AiDebugger.Publisher;

public interface IPublisher
{
    Task<PublishResult> PublishAsync(Finding finding, Fingerprint fingerprint, CancellationToken ct = default);
}

public class PublishResult
{
    public bool Created { get; set; }
    public string? IssueKey { get; set; }
    public string? Url { get; set; }
}
