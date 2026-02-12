using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AiDebugger.Packager;

public static class FingerprintBuilder
{
    private static readonly Regex FrameLineRegex = new(
        @"^\s*at\s+([^\s(]+)\s*(\s+in\s+[^\s]+:\s*line\s+\d+)?",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public static (string? exceptionType, List<string> frames) ParseStacktrace(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return (null, new List<string>());
        var lines = message.Split('\n');
        string? type = null;
        var frames = new List<string>();
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.StartsWith("Exception stack trace:", StringComparison.OrdinalIgnoreCase) ||
                t.StartsWith("Thread exception stack trace:", StringComparison.OrdinalIgnoreCase))
            {
                var colon = t.IndexOf(':');
                if (colon >= 0 && colon + 1 < t.Length)
                    type = t[(colon + 1)..].Trim();
                continue;
            }
            if (t.StartsWith("at ", StringComparison.OrdinalIgnoreCase))
            {
                var m = FrameLineRegex.Match(t);
                if (m.Success && m.Groups.Count >= 2)
                    frames.Add(NormalizeFrame(m.Groups[1].Value));
            }
        }
        if (string.IsNullOrEmpty(type) && frames.Count > 0)
        {
            var first = lines.FirstOrDefault(l => l.Contains("Exception"));
            if (!string.IsNullOrEmpty(first))
                type = first.Trim();
        }
        return (type ?? "Unknown", frames.Take(10).ToList());
    }

    public static string ComputeFingerprint(string exceptionType, IReadOnlyList<string> topFrames)
    {
        var sb = new StringBuilder();
        sb.Append(exceptionType ?? "Unknown");
        for (var i = 0; i < Math.Min(5, topFrames?.Count ?? 0); i++)
            sb.Append('|').Append(topFrames![i]);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }

    private static string NormalizeFrame(string frame)
    {
        return frame
            .Replace(" in E:\\agents\\vsts-agent-win-x64-3.238.0\\_work\\1\\s\\", " ")
            .Replace(" in ", " ")
            .Trim();
    }
}
