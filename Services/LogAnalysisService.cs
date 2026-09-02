using LogAnalyzerAI.Models;

namespace LogAnalyzerAI.Services;

public class LogAnalysisService : ILogAnalysisService
{
    public LogAnalysisResult AnalyzeLogs(List<LogEntry> entries)
    {
        if (entries == null || !entries.Any())
        {
            return new LogAnalysisResult();
        }

        // LINQ: Log level distribution
        var logLevelCounts = entries
            .GroupBy(e => e.LogLevel)
            .ToDictionary(g => g.Key, g => g.Count());

        int infoCount = logLevelCounts.GetValueOrDefault("INFO", 0);
        int warnCount = logLevelCounts.GetValueOrDefault("WARN", 0);
        int errorCount = logLevelCounts.GetValueOrDefault("ERROR", 0) + logLevelCounts.GetValueOrDefault("FATAL", 0);

        // LINQ: Top 5 most frequent error messages
        var topErrors = entries
            .Where(e => e.LogLevel == "ERROR" || e.LogLevel == "FATAL")
            .GroupBy(e => TruncateMessage(e.Message))
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new TopErrorItem
            {
                ErrorMessage = g.Key,
                Count = g.Count()
            })
            .ToList();

        // LINQ: Top 5 sources producing errors/warnings
        var topSources = entries
            .Where(e => e.LogLevel == "ERROR" || e.LogLevel == "WARN" || e.LogLevel == "FATAL")
            .GroupBy(e => e.Source)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new TopSourceItem
            {
                Source = g.Key,
                Count = g.Count()
            })
            .ToList();

        // LINQ: Distinct sample error logs for AI prompt context
        var sampleErrorLogs = entries
            .Where(e => e.LogLevel == "ERROR" || e.LogLevel == "FATAL")
            .DistinctBy(e => e.Message)
            .Take(5)
            .ToList();

        return new LogAnalysisResult
        {
            TotalLogCount = entries.Count,
            InfoCount = infoCount,
            WarningCount = warnCount,
            ErrorCount = errorCount,
            LogLevelCounts = logLevelCounts,
            TopErrors = topErrors,
            TopSources = topSources,
            SampleErrorLogs = sampleErrorLogs
        };
    }

    private static string TruncateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Empty error message";
        return message.Length > 120 ? message[..120] + "..." : message;
    }
}
