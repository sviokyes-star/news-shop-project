@echo off
echo Компиляция Infinite Round плагина...

cd /d "%~dp0"

dotnet restore
dotnet build -c Release

if %errorlevel% equ 0 (
    echo Плагин успешно скомпилирован!
    echo Файл находится в: bin\Release\net8.0\InfiniteRoundPlugin.dll
) else (
    echo Ошибка компиляции!
    exit /b 1
)

pause
