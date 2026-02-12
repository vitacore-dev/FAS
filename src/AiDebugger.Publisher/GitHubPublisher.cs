using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiDebugger.Storage.Entities;

namespace AiDebugger.Publisher;

public class GitHubPublisher : IPublisher
{
    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;
    private readonly List<string> _labels;

    public GitHubPublisher(HttpClient http, string owner, string repo, List<string> labels)
    {
        _http = http;
        _owner = owner;
        _repo = repo;
        _labels = labels ?? new List<string>();
    }

    public async Task<PublishResult> PublishAsync(Finding finding, Fingerprint fingerprint, CancellationToken ct = default)
    {
        var title = $"[AI Debugger] {fingerprint.ExceptionType}";
        var body = BuildBody(finding, fingerprint);
        var request = new
        {
            title,
            body,
            labels = _labels
        };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(
            $"https://api.github.com/repos/{_owner}/{_repo}/issues",
            content,
            ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return new PublishResult { Created = false };

        var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(responseBody);
        var number = doc.RootElement.GetProperty("number").GetInt32();
        var url = doc.RootElement.TryGetProperty("html_url", out var u) ? u.GetString() : null;
        return new PublishResult
        {
            Created = true,
            IssueKey = number.ToString(),
            Url = url
        };
    }

    private static string BuildBody(Finding finding, Fingerprint fingerprint)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Summary");
        sb.AppendLine($"**Fingerprint:** `{fingerprint.FingerprintId}`");
        sb.AppendLine($"**Exception:** " + fingerprint.ExceptionType);
        sb.AppendLine();
        sb.AppendLine("## Root cause / chain");
        sb.AppendLine("```json");
        sb.AppendLine(finding.RootCauseChainJson ?? "{}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Suggested fix");
        sb.AppendLine(finding.SuggestedFixJson ?? "-");
        return sb.ToString();
    }
}
