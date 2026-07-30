#!/bin/bash

echo "Компиляция CS2 Spawn Protect плагина..."

cd "$(dirname "$0")"

echo "Лог сборки: build_error.txt"

dotnet restore > build_error.txt 2>&1
dotnet build -c Release >> build_error.txt 2>&1

if [ $? -eq 0 ]; then
    echo "Плагин успешно скомпилирован!"
    echo "Файл находится в: bin/Release/net8.0/SpawnProtectPlugin.dll"
else
    echo "ОШИБКА КОМПИЛЯЦИИ! Подробности в файле build_error.txt"
    echo "--- Строки с ошибками: ---"
    grep -i "error" build_error.txt
    exit 1
fi
