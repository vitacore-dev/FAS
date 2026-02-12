using System.Text.RegularExpressions;

namespace AiDebugger.Retrieval;

public class GitRetrievalService : IRetrievalService
{
    private readonly string _repoPath;

    public GitRetrievalService(string repoPath)
    {
        _repoPath = repoPath ?? throw new ArgumentNullException(nameof(repoPath));
    }

    public async Task<SearchRepoResult> SearchRepoAsync(SearchRepoRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_repoPath) || !Directory.Exists(_repoPath))
            return new SearchRepoResult();

        var query = request.Query.Trim();
        if (string.IsNullOrEmpty(query)) return new SearchRepoResult();

        var pattern = new Regex(Regex.Escape(query), RegexOptions.IgnoreCase);
        var results = new List<SearchMatch>();
        var glob = request.FileGlob ?? "*.*";
        var ext = glob.Contains("*.cs") ? "*.cs" : "*.*";
        var searchDir = string.IsNullOrEmpty(request.PathPrefix)
            ? _repoPath
            : Path.Combine(_repoPath, request.PathPrefix.Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(searchDir)) return new SearchRepoResult();

        foreach (var file in Directory.EnumerateFiles(searchDir, ext, new EnumerationOptions { RecurseSubdirectories = true }))
        {
            if (results.Count >= request.MaxResults) break;
            ct.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(_repoPath, file);
            if (relativePath.Contains("node_modules") || relativePath.Contains(".git")) continue;
            var lines = await File.ReadAllLinesAsync(file, ct).ConfigureAwait(false);
            for (var i = 0; i < lines.Length && results.Count < request.MaxResults; i++)
            {
                if (!pattern.IsMatch(lines[i])) continue;
                var preview = lines[i].Trim();
                if (preview.Length > 120) preview = preview[..120] + "...";
                results.Add(new SearchMatch
                {
                    Path = relativePath.Replace(Path.DirectorySeparatorChar, '/'),
                    Kind = "match",
                    Score = 1.0,
                    Preview = preview,
                    StartLine = i + 1,
                    EndLine = i + 1
                });
            }
        }
        return new SearchRepoResult { Results = results };
    }

    public async Task<FetchSnippetResult> FetchSnippetAsync(FetchSnippetRequest request, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_repoPath, request.Path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return new FetchSnippetResult { Path = request.Path, StartLine = request.StartLine, EndLine = request.EndLine, Content = "" };

        var lines = await File.ReadAllLinesAsync(fullPath, ct).ConfigureAwait(false);
        var start = Math.Max(0, request.StartLine - 1);
        var end = Math.Min(lines.Length, request.EndLine);
        if (start >= end) return new FetchSnippetResult { Path = request.Path, StartLine = request.StartLine, EndLine = request.EndLine, Content = "" };
        var content = string.Join(Environment.NewLine, lines[start..end]);
        return new FetchSnippetResult
        {
            Path = request.Path,
            StartLine = start + 1,
            EndLine = end,
            Content = content
        };
    }
}
