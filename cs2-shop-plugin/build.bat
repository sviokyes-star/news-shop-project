@echo off
echo Building Shop Plugin...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)

echo Build successful!
echo Output: bin\Release\net8.0\cs2-shop-plugin.dll
pause
