@echo off
echo Компиляция CS2 No Map Music плагина...

cd /d "%~dp0"

dotnet restore
dotnet build -c Release

if %errorlevel% equ 0 (
    echo Плагин успешно скомпилирован!
    echo Файл находится в: bin\Release\net8.0\NoMapMusicPlugin.dll
) else (
    echo Ошибка компиляции!
    exit /b 1
)

pause
