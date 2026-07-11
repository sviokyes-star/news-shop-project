@echo off
echo Компиляция CS2 Triple Jump плагина...

cd /d "%~dp0"

dotnet restore
dotnet build -c Release

if %errorlevel% equ 0 (
    echo Плагин успешно скомпилирован!
    echo Файл находится в: bin\Release\net8.0\TripleJumpPlugin.dll
) else (
    echo Ошибка компиляции!
    exit /b 1
)

pause
