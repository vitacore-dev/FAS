namespace AiDebugger.Retrieval;

public interface IRetrievalService
{
    Task<SearchRepoResult> SearchRepoAsync(SearchRepoRequest request, CancellationToken ct = default);
    Task<FetchSnippetResult> FetchSnippetAsync(FetchSnippetRequest request, CancellationToken ct = default);
}

public class SearchRepoRequest
{
    public string RepoId { get; set; } = "default";
    public string Query { get; set; } = "";
    public string? PathPrefix { get; set; }
    public string? FileGlob { get; set; }
    public int MaxResults { get; set; } = 20;
}

public class SearchRepoResult
{
    public List<SearchMatch> Results { get; set; } = new();
}

public class SearchMatch
{
    public string Path { get; set; } = "";
    public string Kind { get; set; } = "match";
    public double Score { get; set; } = 1.0;
    public string? Preview { get; set; }
    public int? StartLine { get; set; }
    public int? EndLine { get; set; }
}

public class FetchSnippetRequest
{
    public string RepoId { get; set; } = "default";
    public string Path { get; set; } = "";
    public int StartLine { get; set; }
    public int EndLine { get; set; }
}

public class FetchSnippetResult
{
    public string Path { get; set; } = "";
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Content { get; set; } = "";
}
