using LogAnalyzerAI.Models;

namespace LogAnalyzerAI.Services;

public interface ILogParserService
{
    List<LogEntry> ParseLogStream(Stream stream);
    List<LogEntry> ParseLogContent(string content);
}
