#!/bin/bash

echo "🔨 Компиляция CS2 Bhop плагина..."

cd cs2-bhop-plugin

dotnet restore
dotnet build -c Release

if [ $? -eq 0 ]; then
    echo "✅ Плагин успешно скомпилирован!"
    echo "📦 Файл находится в: cs2-bhop-plugin/bin/Release/net8.0/BhopPlugin.dll"
    
    # Создаём ZIP архив для удобства
    cd bin/Release/net8.0
    zip -r ../../../BhopPlugin-Release.zip .
    cd ../../../
    
    echo "📦 ZIP архив создан: cs2-bhop-plugin/BhopPlugin-Release.zip"
else
    echo "❌ Ошибка компиляции!"
    exit 1
fi
