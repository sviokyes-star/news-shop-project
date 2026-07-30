@echo off
echo Компиляция Chat Filter [Okyes]...

cd /d "%~dp0"

dotnet restore
dotnet build -c Release > build_log.txt 2>&1

if %errorlevel% equ 0 (
    echo.
    echo Плагин успешно скомпилирован!
    echo Файл находится в: bin\Release\net8.0\ChatFilterPlugin.dll
) else (
    echo.
    echo ============================================
    echo ОШИБКА КОМПИЛЯЦИИ! Текст ошибки ниже:
    echo ============================================
    type build_log.txt
    echo ============================================
    echo Лог также сохранён в файл: build_log.txt
    echo ============================================
)

echo.
pause
