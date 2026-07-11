#!/bin/bash

echo "🔨 Компиляция Admin [Okyes]..."

cd "$(dirname "$0")"

dotnet restore
dotnet build -c Release

if [ $? -eq 0 ]; then
    echo "✅ Плагин успешно скомпилирован!"
    echo "📦 Файл находится в: bin/Release/net8.0/AdminOkyesPlugin.dll"

    cd bin/Release/net8.0
    zip -r ../../../AdminOkyesPlugin-Release.zip .
    cd ../../../

    echo "📦 ZIP архив создан: AdminOkyesPlugin-Release.zip"
else
    echo "❌ Ошибка компиляции!"
    exit 1
fi
