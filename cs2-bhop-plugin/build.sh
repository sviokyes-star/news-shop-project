#!/bin/bash

echo "🔨 Компиляция CS2 Bhop плагина..."

cd "$(dirname "$0")"

dotnet restore
dotnet build -c Release

if [ $? -eq 0 ]; then
    echo "✅ Плагин успешно скомпилирован!"
    echo "📦 Файл находится в: bin/Release/net8.0/BhopPlugin.dll"

    cd bin/Release/net8.0
    zip -r ../../../BhopPlugin-Release.zip .
    cd ../../../

    echo "📦 ZIP архив создан: BhopPlugin-Release.zip"
else
    echo "❌ Ошибка компиляции!"
    exit 1
fi
