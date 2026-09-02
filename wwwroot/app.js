document.addEventListener("DOMContentLoaded", () => {
    const dropZone = document.getElementById("dropZone");
    const fileInput = document.getElementById("fileInput");
    const fileInfo = document.getElementById("fileInfo");
    const fileNameDisplay = document.getElementById("fileName");
    const btnAnalyze = document.getElementById("btnAnalyze");
    const dashboardSection = document.getElementById("dashboardSection");
    const btnGenerateAi = document.getElementById("btnGenerateAi");
    const aiReportSection = document.getElementById("aiReportSection");
    const aiLoading = document.getElementById("aiLoading");
    const aiReportOutput = document.getElementById("aiReportOutput");
    const modelSelect = document.getElementById("modelSelect");
    const aiStatusBadge = document.getElementById("aiStatusBadge");

    let currentFile = null;
    let currentAnalysisResult = null;

    // Fetch models on load
    fetchModels();

    // Drag and drop handlers
    dropZone.addEventListener("click", () => fileInput.click());

    dropZone.addEventListener("dragover", (e) => {
        e.preventDefault();
        dropZone.classList.add("dragover");
    });

    dropZone.addEventListener("dragleave", () => {
        dropZone.classList.remove("dragover");
    });

    dropZone.addEventListener("drop", (e) => {
        e.preventDefault();
        dropZone.classList.remove("dragover");
        if (e.dataTransfer.files.length > 0) {
            handleFileSelection(e.dataTransfer.files[0]);
        }
    });

    fileInput.addEventListener("change", (e) => {
        if (e.target.files.length > 0) {
            handleFileSelection(e.target.files[0]);
        }
    });

    function handleFileSelection(file) {
        currentFile = file;
        fileNameDisplay.textContent = `${file.name} (${(file.size / 1024).toFixed(1)} KB)`;
        fileInfo.classList.remove("hidden");
    }

    // Analyze Button Click
    btnAnalyze.addEventListener("click", async () => {
        if (!currentFile) return;

        btnAnalyze.disabled = true;
        btnAnalyze.textContent = "⏳ Analiz Ediliyor...";

        const formData = new FormData();
        formData.append("file", currentFile);

        try {
            const response = await fetch("/api/log/analyze", {
                method: "POST",
                body: formData
            });

            if (!response.ok) {
                const err = await response.json();
                alert("Hata: " + (err.message || "Analiz yapılamadı."));
                return;
            }

            currentAnalysisResult = await response.json();
            renderDashboard(currentAnalysisResult);

            dashboardSection.classList.remove("hidden");
            dashboardSection.scrollIntoView({ behavior: "smooth" });
        } catch (error) {
            alert("Sunucu ile iletişim hatası: " + error.message);
        } finally {
            btnAnalyze.disabled = false;
            btnAnalyze.textContent = "⚡ Logları Analiz Et";
        }
    });

    // Render Dashboard Cards & Lists
    function renderDashboard(data) {
        document.getElementById("statTotal").textContent = data.totalLogCount;
        document.getElementById("statInfo").textContent = data.infoCount;
        document.getElementById("statWarn").textContent = data.warningCount;
        document.getElementById("statError").textContent = data.errorCount;

        // Top Errors List
        const topErrorsList = document.getElementById("topErrorsList");
        topErrorsList.innerHTML = "";
        if (data.topErrors && data.topErrors.length > 0) {
            data.topErrors.forEach(err => {
                const li = document.createElement("li");
                li.innerHTML = `<span>${escapeHtml(err.errorMessage)}</span> <span class="count-badge">${err.count} kez</span>`;
                topErrorsList.appendChild(li);
            });
        } else {
            topErrorsList.innerHTML = "<li><em>Hata kaydı bulunamadı.</em></li>";
        }

        // Top Sources List
        const topSourcesList = document.getElementById("topSourcesList");
        topSourcesList.innerHTML = "";
        if (data.topSources && data.topSources.length > 0) {
            data.topSources.forEach(src => {
                const li = document.createElement("li");
                li.innerHTML = `<span><strong>${escapeHtml(src.source)}</strong></span> <span class="count-badge">${src.count} olay</span>`;
                topSourcesList.appendChild(li);
            });
        } else {
            topSourcesList.innerHTML = "<li><em>Kaynak kaydı bulunamadı.</em></li>";
        }
    }

    // AI Generate Button Click
    btnGenerateAi.addEventListener("click", async () => {
        if (!currentAnalysisResult) return;

        const selectedModel = modelSelect.value;
        aiStatusBadge.textContent = `Model: ${selectedModel}`;

        aiReportSection.classList.remove("hidden");
        aiLoading.classList.remove("hidden");
        aiReportOutput.innerHTML = "";
        aiReportSection.scrollIntoView({ behavior: "smooth" });

        btnGenerateAi.disabled = true;

        try {
            const response = await fetch("/api/log/ai-summary", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    analysisResult: currentAnalysisResult,
                    modelId: selectedModel
                })
            });

            const data = await response.json();
            if (!response.ok) {
                aiReportOutput.innerHTML = `<div class="error-box">❌ ${escapeHtml(data.message || "AI Raporu alınamadı.")}</div>`;
                return;
            }

            // Render Markdown
            if (window.marked) {
                aiReportOutput.innerHTML = marked.parse(data.summary);
            } else {
                aiReportOutput.textContent = data.summary;
            }
        } catch (error) {
            aiReportOutput.innerHTML = `<div class="error-box">❌ Bağlantı Hatası: ${escapeHtml(error.message)}</div>`;
        } finally {
            aiLoading.classList.add("hidden");
            btnGenerateAi.disabled = false;
        }
    });

    async function fetchModels() {
        try {
            const res = await fetch("/api/log/models");
            if (res.ok) {
                const models = await res.json();
                if (Array.isArray(models) && models.length > 0) {
                    modelSelect.innerHTML = "";
                    models.forEach(model => {
                        const opt = document.createElement("option");
                        opt.value = model;
                        opt.textContent = model;
                        if (model.includes("qwen3.8") || model.includes("qwen-3.8")) {
                            opt.selected = true;
                        }
                        modelSelect.appendChild(opt);
                    });
                }
            }
        } catch (e) {
            console.log("Model listesi çekilemedi, varsayılan modeller kullanılıyor.");
        }
    }

    function escapeHtml(text) {
        if (!text) return "";
        return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    }
});
