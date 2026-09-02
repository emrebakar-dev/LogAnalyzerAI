using LogAnalyzerAI.Models;
using LogAnalyzerAI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers & Options Pattern Configuration
builder.Services.AddControllers();
builder.Services.Configure<GroqSettings>(builder.Configuration.GetSection("GroqSettings"));

// Register Application Services (Dependency Injection)
builder.Services.AddScoped<ILogParserService, LogParserService>();
builder.Services.AddScoped<ILogAnalysisService, LogAnalysisService>();
builder.Services.AddHttpClient<IGroqAIService, GroqAIService>();

var app = builder.Build();

// Configure Middleware Pipeline in correct security & execution order
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Serve wwwroot UI (index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
