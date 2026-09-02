namespace LogAnalyzerAI.Models;

public class LogEntry
{
    public DateTime? Timestamp { get; set; }
    public string LogLevel { get; set; } = "INFO";
    public string Source { get; set; } = "System";
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string RawLine { get; set; } = string.Empty;
}
