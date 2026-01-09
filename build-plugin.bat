@echo off
echo Компиляция CS2 Bhop плагина...

cd cs2-bhop-plugin

dotnet restore
dotnet build -c Release

if %errorlevel% equ 0 (
    echo Плагин успешно скомпилирован!
    echo Файл находится в: cs2-bhop-plugin\bin\Release\net8.0\BhopPlugin.dll
) else (
    echo Ошибка компиляции!
    exit /b 1
)

pause
