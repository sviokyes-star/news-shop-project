@echo off
echo Компиляция Rock The Vote...

cd /d "%~dp0"

dotnet restore
dotnet build -c Release

if %errorlevel% equ 0 (
    echo Плагин успешно скомпилирован!
    echo Файл находится в: bin\Release\net8.0\RtvPlugin.dll
) else (
    echo Ошибка компиляции!
    exit /b 1
)

pause
