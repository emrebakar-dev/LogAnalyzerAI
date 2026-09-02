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
    const errorMessageBanner = document.getElementById("errorMessage");

    const MAX_FILE_SIZE = 15 * 1024 * 1024; // 15 MB
    const ALLOWED_EXTENSIONS = [".log", ".txt"];

    let currentFile = null;
    let currentAnalysisResult = null;

    // Fetch available models safely on load
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
        hideError();

        // Client-side file validation
        if (file.size > MAX_FILE_SIZE) {
            showError("Seçilen dosya çok büyük. Lütfen 15 MB'tan küçük bir dosya seçin.");
            return;
        }

        const ext = file.name.substring(file.name.lastIndexOf(".")).toLowerCase();
        if (!ALLOWED_EXTENSIONS.includes(ext)) {
            showError("Desteklenmeyen dosya türü. Lütfen .log veya .txt dosyası yükleyin.");
            return;
        }

        currentFile = file;
        fileNameDisplay.textContent = `${file.name} (${(file.size / 1024).toFixed(1)} KB)`;
        fileInfo.classList.remove("hidden");
    }

    // Analyze Button Click
    btnAnalyze.addEventListener("click", async () => {
        if (!currentFile) return;

        hideError();
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
                showError(err.message || "Analiz yapılamadı.");
                return;
            }

            currentAnalysisResult = await response.json();
            renderDashboard(currentAnalysisResult);

            dashboardSection.classList.remove("hidden");
            dashboardSection.scrollIntoView({ behavior: "smooth" });
        } catch (error) {
            showError("Sunucu ile iletişim kurulurken bir hata oluştu.");
        } finally {
            btnAnalyze.disabled = false;
            btnAnalyze.textContent = "⚡ Logları Analiz Et";
        }
    });

    // Render Dashboard Cards & Lists using safe DOM manipulation
    function renderDashboard(data) {
        document.getElementById("statTotal").textContent = data.totalLogCount || 0;
        document.getElementById("statInfo").textContent = data.infoCount || 0;
        document.getElementById("statWarn").textContent = data.warningCount || 0;
        document.getElementById("statError").textContent = data.errorCount || 0;

        // Top Errors List (Safe DOM creation)
        const topErrorsList = document.getElementById("topErrorsList");
        clearContainer(topErrorsList);

        if (data.topErrors && data.topErrors.length > 0) {
            data.topErrors.forEach(err => {
                const li = document.createElement("li");

                const spanMsg = document.createElement("span");
                spanMsg.textContent = err.errorMessage;

                const spanBadge = document.createElement("span");
                spanBadge.className = "count-badge";
                spanBadge.textContent = `${err.count} kez`;

                li.appendChild(spanMsg);
                li.appendChild(spanBadge);
                topErrorsList.appendChild(li);
            });
        } else {
            const li = document.createElement("li");
            const em = document.createElement("em");
            em.textContent = "Hata kaydı bulunamadı.";
            li.appendChild(em);
            topErrorsList.appendChild(li);
        }

        // Top Sources List (Safe DOM creation)
        const topSourcesList = document.getElementById("topSourcesList");
        clearContainer(topSourcesList);

        if (data.topSources && data.topSources.length > 0) {
            data.topSources.forEach(src => {
                const li = document.createElement("li");

                const spanSrc = document.createElement("span");
                const strong = document.createElement("strong");
                strong.textContent = src.source;
                spanSrc.appendChild(strong);

                const spanBadge = document.createElement("span");
                spanBadge.className = "count-badge";
                spanBadge.textContent = `${src.count} olay`;

                li.appendChild(spanSrc);
                li.appendChild(spanBadge);
                topSourcesList.appendChild(li);
            });
        } else {
            const li = document.createElement("li");
            const em = document.createElement("em");
            em.textContent = "Kaynak kaydı bulunamadı.";
            li.appendChild(em);
            topSourcesList.appendChild(li);
        }
    }

    // AI Generate Button Click
    btnGenerateAi.addEventListener("click", async () => {
        if (!currentAnalysisResult) return;

        hideError();
        const selectedModel = modelSelect.value;
        aiStatusBadge.textContent = `Model: ${selectedModel}`;

        aiReportSection.classList.remove("hidden");
        aiLoading.classList.remove("hidden");
        clearContainer(aiReportOutput);
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
                renderErrorBox(aiReportOutput, data.message || "AI Raporu alınamadı.");
                return;
            }

            // Render Markdown safely
            if (window.marked && typeof window.marked.parse === "function") {
                const parsedHtml = window.marked.parse(data.summary);
                aiReportOutput.innerHTML = parsedHtml;
            } else {
                aiReportOutput.textContent = data.summary;
            }
        } catch (error) {
            renderErrorBox(aiReportOutput, "Sunucu ile bağlantı kurulamadı.");
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
                    clearContainer(modelSelect);
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
            console.warn("Model listesi alınamadı, varsayılan listeyle devam ediliyor.");
        }
    }

    function clearContainer(container) {
        while (container.firstChild) {
            container.removeChild(container.firstChild);
        }
    }

    function showError(msg) {
        if (errorMessageBanner) {
            errorMessageBanner.textContent = `⚠️ ${msg}`;
            errorMessageBanner.classList.remove("hidden");
        }
    }

    function hideError() {
        if (errorMessageBanner) {
            errorMessageBanner.textContent = "";
            errorMessageBanner.classList.add("hidden");
        }
    }

    function renderErrorBox(container, msg) {
        clearContainer(container);
        const div = document.createElement("div");
        div.className = "error-box";
        div.textContent = `❌ ${msg}`;
        container.appendChild(div);
    }
});
