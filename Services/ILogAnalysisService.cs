using LogAnalyzerAI.Models;

namespace LogAnalyzerAI.Services;

public interface ILogAnalysisService
{
    LogAnalysisResult AnalyzeLogs(List<LogEntry> entries);
}
