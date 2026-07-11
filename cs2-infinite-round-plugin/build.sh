#!/bin/bash

echo "🔨 Компиляция Infinite Round плагина..."

cd "$(dirname "$0")"

dotnet restore
dotnet build -c Release

if [ $? -eq 0 ]; then
    echo "✅ Плагин успешно скомпилирован!"
    echo "📦 Файл находится в: bin/Release/net8.0/InfiniteRoundPlugin.dll"
else
    echo "❌ Ошибка компиляции!"
    exit 1
fi
