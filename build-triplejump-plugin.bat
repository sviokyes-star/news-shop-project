@echo off
echo Компиляция CS2 Triple Jump плагина...

cd cs2-triplejump-plugin

dotnet restore
dotnet build -c Release

if %errorlevel% equ 0 (
    echo Плагин успешно скомпилирован!
    echo Файл находится в: cs2-triplejump-plugin\bin\Release\net8.0\TripleJumpPlugin.dll
) else (
    echo Ошибка компиляции!
    exit /b 1
)

pause
