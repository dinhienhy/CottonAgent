namespace CBAS.Web.DTOs;

public class ClaudeParseLog
{
    public bool Used { get; set; }
    public string Status { get; set; } = "idle"; // idle, processing, success, error, fallback
    public string? Model { get; set; }
    public string? ErrorMessage { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? CacheCreationTokens { get; set; }
    public int? CacheReadTokens { get; set; }
    public string? StopReason { get; set; }
    public string? RawResponse { get; set; }
    public int? LotsFound { get; set; }
    public double ElapsedSeconds { get; set; }
    public bool HasFewShot { get; set; }
    public bool FellBackToRegex { get; set; }

    public List<string> Steps { get; set; } = new();

    public void AddStep(string msg)
    {
        Steps.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
    }
}
