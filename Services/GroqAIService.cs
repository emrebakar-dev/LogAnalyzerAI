using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using LogAnalyzerAI.Models;

namespace LogAnalyzerAI.Services;

public class GroqAIService : IGroqAIService
{
    private readonly HttpClient _httpClient;
    private readonly GroqSettings _settings;
    private readonly ILogger<GroqAIService> _logger;

    public GroqAIService(HttpClient httpClient, IOptions<GroqSettings> settingsOptions, ILogger<GroqAIService> logger)
    {
        _httpClient = httpClient;
        _settings = settingsOptions.Value;
        _logger = logger;
    }

    public async Task<string> GenerateSummaryAsync(LogAnalysisResult analysis, string? requestedModel = null)
    {
        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = _settings.ApiKey;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "⚠️ **GROQ_API_KEY Ortam Değişkeni Bulunamadı!**\n\nLütfen terminalinizde veya çalışma ortamınızda `export GROQ_API_KEY=\"gsk_...\"` komutu ile API anahtarınızı tanımlayın.";
        }

        var selectedModel = !string.IsNullOrWhiteSpace(requestedModel)
            ? requestedModel
            : (!string.IsNullOrWhiteSpace(_settings.ModelId) ? _settings.ModelId : "qwen/qwen3.8-27b");

        var promptPayload = BuildPromptPayload(analysis);

        var requestBody = new
        {
            model = selectedModel,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "Sen Kıdemli C# ve ASP.NET Core Sistem Güvenilirlik Mühendisisin (SRE). Sana sunulan önceden işlenmiş ve özetlenmiş log istatistiklerini ve örnek hata mesajlarını analiz et. Türkçe dilinde net, kurumsal ve anlaşılır bir Kök Neden (Root Cause) ve Çözüm Önerisi Raporu hazırla. Yanıtını Markdown formatında ver."
                },
                new
                {
                    role = "user",
                    content = promptPayload
                }
            },
            temperature = 0.3,
            max_tokens = 2048
        };

        try
        {
            var requestJson = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Groq API Error: {StatusCode} - {Body}", response.StatusCode, responseJson);
                return $"❌ **Groq API Hata Döndürdü ({response.StatusCode}):**\n```json\n{responseJson}\n```";
            }

            using var doc = JsonDocument.Parse(responseJson);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? "AI yanıtı boş döndü.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq API çağrısı sırasında hata oluştu.");
            return $"❌ **AI Analiz Hatası:** {ex.Message}";
        }
    }

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        var rawModels = new List<string>();
        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = _settings.ApiKey;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new List<string> { "qwen/qwen3.8-27b", "openai/gpt-oss-120b", "qwen/qwen3.6-27b", "llama-3.3-70b-versatile" };
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.BaseUrl.TrimEnd('/')}/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var data = doc.RootElement.GetProperty("data");

                foreach (var element in data.EnumerateArray())
                {
                    if (element.TryGetProperty("id", out var idProp))
                    {
                        var id = idProp.GetString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            rawModels.Add(id);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Groq aktif model listesi çekilemedi.");
        }

        // Filter out audio (whisper) and guard/safeguard models
        var chatModels = rawModels
            .Where(m => !m.Contains("whisper", StringComparison.OrdinalIgnoreCase) &&
                        !m.Contains("guard", StringComparison.OrdinalIgnoreCase) &&
                        !m.Contains("orpheus", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Contains("qwen3.8", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(m => m.Contains("gpt-oss-120b", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(m => m.Contains("qwen3.6", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(m => m.Contains("70b", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!chatModels.Any())
        {
            chatModels = new List<string> { "qwen/qwen3.8-27b", "openai/gpt-oss-120b", "qwen/qwen3.6-27b" };
        }

        return chatModels;
    }

    private static string BuildPromptPayload(LogAnalysisResult analysis)
    {
        var sb = new StringBuilder();
        sb.AppendLine("### 📊 LOG ANALİZ VE İSTATİSTİK VERİLERİ");
        sb.AppendLine($"- **Toplam Log Satırı:** {analysis.TotalLogCount}");
        sb.AppendLine($"- **INFO Sayısı:** {analysis.InfoCount}");
        sb.AppendLine($"- **WARNING Sayısı:** {analysis.WarningCount}");
        sb.AppendLine($"- **ERROR / FATAL Sayısı:** {analysis.ErrorCount}");
        sb.AppendLine();

        sb.AppendLine("### 🚨 EN SIK TEKRAR EDEN HATALAR (TOP ERRORS)");
        if (analysis.TopErrors.Any())
        {
            foreach (var err in analysis.TopErrors)
            {
                sb.AppendLine($"- **[{err.Count} Kez Tekrarlandı]**: {err.ErrorMessage}");
            }
        }
        else
        {
            sb.AppendLine("Kayıtlı kritik hata bulunamadı.");
        }
        sb.AppendLine();

        sb.AppendLine("### 📍 EN ÇOK HATA ÜRETEN KAYNAKLAR (TOP SOURCES)");
        if (analysis.TopSources.Any())
        {
            foreach (var src in analysis.TopSources)
            {
                sb.AppendLine($"- **{src.Source}**: {src.Count} olay");
            }
        }
        sb.AppendLine();

        sb.AppendLine("### 🔍 ÖRNEK HATA MESAJLARI VE STACK TRACE'LER");
        if (analysis.SampleErrorLogs.Any())
        {
            foreach (var log in analysis.SampleErrorLogs)
            {
                sb.AppendLine($"**[Hata Seviyesi: {log.LogLevel}] [Kaynak: {log.Source}] [Zaman: {log.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Bilinmiyor"}]**");
                sb.AppendLine($"Mesaj: {log.Message}");
                if (!string.IsNullOrWhiteSpace(log.StackTrace))
                {
                    sb.AppendLine("Stack Trace:");
                    sb.AppendLine("```");
                    sb.AppendLine(log.StackTrace);
                    sb.AppendLine("```");
                }
                sb.AppendLine("---");
            }
        }
        else
        {
            sb.AppendLine("Detaylı stack trace bulunmamaktadır.");
        }

        sb.AppendLine();
        sb.AppendLine("Lütfen bu verilere dayanarak:");
        sb.AppendLine("1. **Genel Durum Özetini** yap.");
        sb.AppendLine("2. En olası **Kök Nedenleri (Root Cause)** belirle.");
        sb.AppendLine("3. Yazılım geliştirme ve sistem ekibi için **Aksiyon Alınabilir Çözüm Adımlarını** listele.");

        return sb.ToString();
    }
}
