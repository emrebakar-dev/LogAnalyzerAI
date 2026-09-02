namespace LogAnalyzerAI.Models;

public class LogAnalysisResult
{
    public int TotalLogCount { get; set; }
    public int InfoCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<string, int> LogLevelCounts { get; set; } = new();
    public List<TopErrorItem> TopErrors { get; set; } = new();
    public List<TopSourceItem> TopSources { get; set; } = new();
    public List<LogEntry> SampleErrorLogs { get; set; } = new();
}

public class TopErrorItem
{
    public string ErrorMessage { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopSourceItem
{
    public string Source { get; set; } = string.Empty;
    public int Count { get; set; }
}
