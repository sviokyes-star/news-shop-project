#!/bin/bash

echo "Компиляция CS2 No Map Music плагина..."

cd "$(dirname "$0")"

dotnet restore
dotnet build -c Release

if [ $? -eq 0 ]; then
    echo "Плагин успешно скомпилирован!"
    echo "Файл находится в: bin/Release/net8.0/NoMapMusicPlugin.dll"
else
    echo "Ошибка компиляции!"
    exit 1
fi
