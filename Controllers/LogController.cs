using Microsoft.AspNetCore.Mvc;
using LogAnalyzerAI.Models;
using LogAnalyzerAI.Services;

namespace LogAnalyzerAI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogController : ControllerBase
{
    private readonly ILogParserService _parserService;
    private readonly ILogAnalysisService _analysisService;
    private readonly IGroqAIService _groqAIService;
    private readonly ILogger<LogController> _logger;

    public LogController(
        ILogParserService parserService,
        ILogAnalysisService analysisService,
        IGroqAIService groqAIService,
        ILogger<LogController> logger)
    {
        _parserService = parserService;
        _analysisService = analysisService;
        _groqAIService = groqAIService;
        _logger = logger;
    }

    /// <summary>
    /// Log dosyasını yükler, LINQ ile istatistiksel analizini yapar.
    /// </summary>
    [HttpPost("analyze")]
    public IActionResult AnalyzeLogFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Lütfen geçerli bir log dosyası seçin." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var entries = _parserService.ParseLogStream(stream);
            var analysisResult = _analysisService.AnalyzeLogs(entries);

            return Ok(analysisResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log dosyası ayrıştırılırken sunucu hatası oluştu. Dosya Adı: {FileName}", file.FileName);
            return StatusCode(500, new { message = "Log dosyası ayrıştırılırken sunucu tarafında bir hata meydana geldi." });
        }
    }

    /// <summary>
    /// Analiz edilmiş log sonuçlarını alıp Groq AI üzerinden Türkçe kök neden raporu oluşturur.
    /// </summary>
    [HttpPost("ai-summary")]
    public async Task<IActionResult> GenerateAISummary([FromBody] AISummaryApiRequest request)
    {
        if (request?.AnalysisResult == null)
        {
            return BadRequest(new { message = "Geçersiz analiz verisi." });
        }

        try
        {
            var summaryMarkdown = await _groqAIService.GenerateSummaryAsync(request.AnalysisResult, request.ModelId);
            return Ok(new { summary = summaryMarkdown, modelUsed = request.ModelId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Raporu oluşturulurken sunucu tarafında hata meydana geldi.");
            return StatusCode(500, new { message = "AI özet raporu oluşturulurken sunucu tarafında bir hata meydana geldi." });
        }
    }

    /// <summary>
    /// Groq API üzerindeki aktif kullanılabilir modelleri listeler.
    /// </summary>
    [HttpGet("models")]
    public async Task<IActionResult> GetModels()
    {
        try
        {
            var models = await _groqAIService.GetAvailableModelsAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktif model listesi çekilirken sunucu hatası oluştu.");
            return StatusCode(500, new { message = "Aktif model listesi çekilemedi." });
        }
    }
}

public class AISummaryApiRequest
{
    public LogAnalysisResult AnalysisResult { get; set; } = new();
    public string? ModelId { get; set; }
}
