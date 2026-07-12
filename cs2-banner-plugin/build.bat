@echo off
echo Building CS2 Banner Plugin...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)
echo Build completed successfully!
echo Plugin file is in: bin\Release\net8.0\BannerPlugin.dll
echo.
echo Installation:
echo 1. Copy BannerPlugin.dll to: csgo/addons/counterstrikesharp/plugins/BannerPlugin/
echo 2. Restart server or use: css_plugins reload BannerPlugin
pause
