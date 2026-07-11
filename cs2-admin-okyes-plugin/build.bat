@echo off
echo Компиляция Admin [Okyes]...

cd /d "%~dp0"

dotnet restore
dotnet build -c Release

if %errorlevel% equ 0 (
    echo Плагин успешно скомпилирован!
    echo Файл находится в: bin\Release\net8.0\AdminOkyesPlugin.dll
) else (
    echo Ошибка компиляции!
    exit /b 1
)

pause
