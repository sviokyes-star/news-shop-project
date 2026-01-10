@echo off
echo Компиляция CS2 No Warmup плагина...

cd cs2-nowarmup-plugin

dotnet restore
dotnet build -c Release

if %errorlevel% equ 0 (
    echo Плагин успешно скомпилирован!
    echo Файл находится в: cs2-nowarmup-plugin\bin\Release\net8.0\NoWarmupPlugin.dll
) else (
    echo Ошибка компиляции!
    exit /b 1
)

pause
