# ⚡ LogAnalyzer AI - Yapay Zeka Destekli Log Analiz & Kök Neden Platformu

LogAnalyzer AI, kurumsal ve kamu projelerinde yaygın olarak kullanılan **ASP.NET Core (C# .NET 8)** Web API ve Controller mimarisi üzerine inşa edilmiş; sistem loglarını **LINQ** sorguları ile ayrıştırıp istatistik çıkaran ve **Groq Cloud LLM API** (`qwen/qwen3.8-27b`, `openai/gpt-oss-120b`, `llama-3.3-70b-versatile`) üzerinden Türkçe kök neden (Root Cause) ve çözüm raporu sunan modern bir web platformudur.

---

## 🌟 Öne Çıkan Özellikler

- **🏢 Kurumsal Controller Mimarisi:** Clean Code ilkelerine uygun `Controllers/LogController.cs` ve `Services` katmanı.
- **⚡ C# LINQ Analitigi:** Log seviyelerine (INFO, WARN, ERROR) göre dağılım, en sık tekrarlanan ilk 5 hata mesajı (`TopErrors`) ve en sorunlu ilk 5 modül (`TopSources`).
- **🔍 Best-Effort Log Parser:** Serilog, NLog ve standart formatlardaki log dosyalarını ve çok satırlı StackTrace'leri otomatik ayrıştırır.
- **🤖 Groq LLM Entegrasyonu:** Dinamik model seçimi (`qwen/qwen3.8-27b`, `openai/gpt-oss-120b`) ile ışık hızında Türkçe kök neden analizi.
- **🔒 Hassas Veri Güvenligi:** API Key koda veya konfigürasyon dosyalarına yazılmaz; tamamen `GROQ_API_KEY` ortam değişkeninden (Environment Variable) okunur.
- **💻 Yalın & Hızlı Dashboard:** Sürükle-bırak destekli, hafif Vanilla HTML5/JS web arayüzü.

---

## 🛠️ Teknolojiler

- **Backend:** C# .NET 8, ASP.NET Core Web API, LINQ, Options Pattern, HttpClientFactory
- **AI Engine:** Groq Cloud API (`qwen/qwen3.8-27b`, `openai/gpt-oss-120b`, `llama-3.3-70b-versatile`)
- **Frontend:** HTML5, CSS3, Vanilla JavaScript, Marked.js (Markdown Renderer)

---

## 🚀 Kurulum ve Çalıştırma

### 1. Repoyu Klonlayın
```bash
git clone https://github.com/emrebakar-dev/LogAnalyzerAI.git
cd LogAnalyzerAI
```

### 2. Groq API Anahtarını Tanımlayın
```bash
export GROQ_API_KEY="gsk_your_groq_api_key_here"
```

### 3. Uygulamayı Başlatın
```bash
dotnet run
```

Açılan web arayüzünden (`http://localhost:5230` veya `http://localhost:5000`) `SampleLogs/` klasöründeki örnek log dosyalarını yükleyerek test edebilirsiniz.

---

## 📁 Örnek Log Senaryoları (`SampleLogs/`)
- `01_database_and_payment_errors.log`: SQL Zaman aşımı & Ödeme API hataları.
- `02_auth_and_security_failures.log`: Brute-force giriş denemeleri & JWT token hataları.
- `03_microservices_http_outage.log`: HTTP 503 servis kesintileri & RabbitMQ/gRPC hataları.
- `04_clean_system_healthy.log`: Sağlıklı sistem logları.
- `05_disk_memory_resource_exhaustion.log`: Disk doluluğu (IOException) & RAM tüketim krizleri (OutOfMemoryException).
- `06_mixed_malformed_log_format.log`: Karışık log formatları, bozuk satırlar ve esnek parser testi.
- `07_enterprise_mixed_complex_master.log`: Çoklu servisler (Database, Redis, Payment, Brute Force, RabbitMQ, OutOfMemory) içeren kapsamlı master kurumsal log dosyası.
