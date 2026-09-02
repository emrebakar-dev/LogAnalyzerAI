using LogAnalyzerAI.Models;

namespace LogAnalyzerAI.Services;

public interface IGroqAIService
{
    Task<string> GenerateSummaryAsync(LogAnalysisResult analysis, string? requestedModel = null);
    Task<List<string>> GetAvailableModelsAsync();
}
