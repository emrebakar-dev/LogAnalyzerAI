using System.Text;
using System.Text.RegularExpressions;
using LogAnalyzerAI.Models;

namespace LogAnalyzerAI.Services;

public class LogParserService : ILogParserService
{
    private static readonly Regex LogLinePattern1 = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d+)?)\s*\[(?<level>INFO|WARN|WARNING|ERR|ERROR|FATAL|DEBUG|CRITICAL|TRACE)\]\s*(?:\[(?<source>[^\]]+)\])?\s*(?<message>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LogLinePattern2 = new(
        @"^(?<timestamp>\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2})\s+\[(?<level>INFO|WARN|WARNING|ERR|ERROR|FATAL|DEBUG)\]\s+(?<message>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LogLinePattern3 = new(
        @"^(?<level>INFO|WARN|WARNING|ERR|ERROR|FATAL|DEBUG):\s*(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})?\s*(?:\[(?<source>[^\]]+)\])?\s*(?<message>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LogLevelExtractor = new(
        @"\b(?<level>INFO|WARN|WARNING|ERR|ERROR|FATAL|DEBUG|CRITICAL)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<LogEntry> ParseLogContent(string content)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return ParseLogStream(stream);
    }

    public List<LogEntry> ParseLogStream(Stream stream)
    {
        var entries = new List<LogEntry>();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        LogEntry? currentEntry = null;
        StringBuilder? currentStackTraceBuilder = null;
        StringBuilder? currentMessageBuilder = null;
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Check if this line is part of a stack trace for the previous entry
            if (currentEntry != null && (line.TrimStart().StartsWith("at ") || line.TrimStart().StartsWith("---") || line.TrimStart().StartsWith("Exception:")))
            {
                currentStackTraceBuilder ??= new StringBuilder(currentEntry.StackTrace ?? string.Empty);
                if (currentStackTraceBuilder.Length > 0)
                {
                    currentStackTraceBuilder.AppendLine();
                }
                currentStackTraceBuilder.Append(line);
                currentEntry.StackTrace = currentStackTraceBuilder.ToString();
                continue;
            }

            // Attempt best-effort parsing using regex
            var entry = TryParseLine(line);
            if (entry != null)
            {
                currentEntry = entry;
                currentStackTraceBuilder = null;
                currentMessageBuilder = null;
                entries.Add(currentEntry);
            }
            else if (currentEntry != null && !IsNewLogLine(line))
            {
                // Multi-line message append
                currentMessageBuilder ??= new StringBuilder(currentEntry.Message);
                currentMessageBuilder.Append(' ').Append(line.Trim());
                currentEntry.Message = currentMessageBuilder.ToString();
            }
            else
            {
                // Fallback for unparseable raw log line
                currentEntry = CreateFallbackEntry(line);
                currentStackTraceBuilder = null;
                currentMessageBuilder = null;
                entries.Add(currentEntry);
            }
        }

        return entries;
    }

    private LogEntry? TryParseLine(string line)
    {
        var match = LogLinePattern1.Match(line);
        if (!match.Success) match = LogLinePattern2.Match(line);
        if (!match.Success) match = LogLinePattern3.Match(line);

        if (match.Success)
        {
            var rawLevel = match.Groups["level"].Value.ToUpperInvariant();
            var normalizedLevel = NormalizeLogLevel(rawLevel);

            DateTime? ts = null;
            if (DateTime.TryParse(match.Groups["timestamp"].Value, out var parsedTs))
            {
                ts = parsedTs;
            }

            var source = match.Groups["source"].Success && !string.IsNullOrWhiteSpace(match.Groups["source"].Value)
                ? match.Groups["source"].Value.Trim()
                : "Application";

            return new LogEntry
            {
                Timestamp = ts,
                LogLevel = normalizedLevel,
                Source = source,
                Message = match.Groups["message"].Value.Trim(),
                RawLine = line
            };
        }

        return null;
    }

    private LogEntry CreateFallbackEntry(string line)
    {
        var levelMatch = LogLevelExtractor.Match(line);
        var level = levelMatch.Success ? NormalizeLogLevel(levelMatch.Groups["level"].Value) : "INFO";

        return new LogEntry
        {
            Timestamp = null,
            LogLevel = level,
            Source = "RawLog",
            Message = line.Trim(),
            RawLine = line
        };
    }

    private static bool IsNewLogLine(string line)
    {
        return LogLevelExtractor.IsMatch(line) || Regex.IsMatch(line, @"^\d{4}[-/.]\d{2}[-/.]\d{2}");
    }

    private static string NormalizeLogLevel(string rawLevel)
    {
        return rawLevel.ToUpperInvariant() switch
        {
            "ERR" => "ERROR",
            "WARNING" => "WARN",
            "CRITICAL" => "FATAL",
            _ => rawLevel.ToUpperInvariant()
        };
    }
}
