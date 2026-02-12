using System.Net.Http.Json;
using System.Text.Json;
using AiDebugger.Packager;
using AiDebugger.Retrieval;

namespace AiDebugger.Orchestrator;

public class OpenAIOrchestrator : ILLMOrchestrator
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly IRetrievalService _retrieval;

    public OpenAIOrchestrator(HttpClient http, string model, IRetrievalService retrieval)
    {
        _http = http;
        _model = model;
        _retrieval = retrieval;
    }

    public async Task<OrchestratorResult?> AnalyzeAsync(EvidenceBundle bundle, string fingerprintId, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var logContext = JsonSerializer.Serialize(new
            {
                bundle.ExceptionCounts,
                SampleStacktraces = bundle.SampleStacktraces.Take(3).ToList(),
                bundle.Service,
                bundle.WindowStart,
                bundle.WindowEnd
            });
            var prompt = $@"You are an expert debugger. Analyze this log evidence and determine the root cause.
Return a JSON object with: root_cause (string), chain (array of exception types in order), severity (low|medium|high|critical), suggested_fix (string).

Evidence:
{logContext}

Respond with only valid JSON.";

            var request = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 1024
            };
            var url = (_http.BaseAddress?.ToString() ?? "https://api.openai.com/").TrimEnd('/') + "/v1/chat/completions";
            var response = await _http.PostAsJsonAsync(url, request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<OpenAIResponse>(ct).ConfigureAwait(false);
            var content = body?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
            if (string.IsNullOrEmpty(content)) return null;

            var codeEvidence = "{}";
            if (bundle.ExceptionCounts.Count > 0)
            {
                var firstEx = bundle.ExceptionCounts.Keys.First();
                var search = await _retrieval.SearchRepoAsync(new SearchRepoRequest { Query = firstEx.Split('.').LastOrDefault() ?? firstEx, MaxResults = 3 }, ct).ConfigureAwait(false);
                if (search.Results.Count > 0)
                {
                    var sn = await _retrieval.FetchSnippetAsync(new FetchSnippetRequest
                    {
                        Path = search.Results[0].Path,
                        StartLine = Math.Max(1, (search.Results[0].StartLine ?? 1) - 5),
                        EndLine = (search.Results[0].EndLine ?? 1) + 15
                    }, ct).ConfigureAwait(false);
                    codeEvidence = JsonSerializer.Serialize(new { file = sn.Path, snippet = sn.Content });
                }
            }

            sw.Stop();
            return new OrchestratorResult
            {
                FingerprintId = fingerprintId,
                Severity = "medium",
                PriorityScore = 0.5m,
                RootCauseChainJson = content,
                LogEvidenceJson = logContext,
                CodeEvidenceJson = codeEvidence,
                SuggestedFixJson = content,
                LlmModel = _model,
                LatencyMs = (int)sw.ElapsedMilliseconds
            };
        }
        catch
        {
            return null;
        }
    }

    private class OpenAIResponse
    {
        public List<OpenAIChoice>? Choices { get; set; }
    }

    private class OpenAIChoice
    {
        public OpenAIMessage? Message { get; set; }
    }

    private class OpenAIMessage
    {
        public string? Content { get; set; }
    }
}
