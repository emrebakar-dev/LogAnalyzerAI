#!/bin/bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$DIR"

export PATH=/Users/emrebakar00/.dotnet:$PATH

echo "================================================"
echo "      LogAnalyzer AI Platformu Başlatılıyor     "
echo "================================================"
echo ""

if [ -z "$GROQ_API_KEY" ]; then
    echo "⚠️  GROQ_API_KEY ortam değişkeni bulunamadı."
    read -p "Lütfen Groq API Key girin (gsk_...): " input_key
    if [ -n "$input_key" ]; then
        export GROQ_API_KEY="$input_key"
    fi
fi

echo ""
echo "🚀 ASP.NET Core sunucusu başlatılıyor..."
echo "Web Arayüzü: http://localhost:5000 (veya gösterilen HTTPS/HTTP portu)"
echo "Durdurmak için: CTRL + C"
echo ""

dotnet run
