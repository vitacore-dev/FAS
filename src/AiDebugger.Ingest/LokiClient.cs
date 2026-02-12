using System.Net.Http.Json;
using System.Text.Json;

namespace AiDebugger.Ingest;

public sealed class LokiClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public LokiClient(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<LokiQueryRangeResponse> QueryRangeAsync(
        string query,
        DateTime start,
        DateTime end,
        int limit = 5000,
        CancellationToken ct = default)
    {
        var nsStart = ToNanoseconds(start);
        var nsEnd = ToNanoseconds(end);
        var url = $"{_baseUrl}/loki/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={nsStart}&end={nsEnd}&limit={limit}&direction=forward";
        var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LokiQueryRangeResponse>(ct).ConfigureAwait(false);
        return body ?? new LokiQueryRangeResponse();
    }

    private static long ToNanoseconds(DateTime dt)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)(dt.ToUniversalTime() - epoch).TotalMilliseconds * 1_000_000;
    }
}

public class LokiQueryRangeResponse
{
    public string Status { get; set; } = "";
    public LokiData? Data { get; set; }
}

public class LokiData
{
    public string ResultType { get; set; } = "";
    public List<LokiStream> Result { get; set; } = new();
}

public class LokiStream
{
    public Dictionary<string, string> Stream { get; set; } = new();
    public List<List<string>> Values { get; set; } = new(); // [ [ "timestamp_ns", "log line" ], ... ]
}
