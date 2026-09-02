using LogAnalyzerAI.Models;

namespace LogAnalyzerAI.Services;

public interface ILogAnalysisService
{
    LogAnalysisResult AnalyzeLogs(IEnumerable<LogEntry> entries);
}
