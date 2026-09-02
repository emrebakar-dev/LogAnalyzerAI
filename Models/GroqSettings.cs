namespace LogAnalyzerAI.Models;

public class GroqSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "llama-3.3-70b-versatile";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/";
    public List<string> PreferredModels { get; set; } = new();
}
