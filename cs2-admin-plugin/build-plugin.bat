@echo off
echo Building CS2 Admin Plugin...

dotnet build -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================
    echo Build successful!
    echo ================================
    echo.
    echo Plugin files are in: bin\Release\net8.0\
    echo.
    echo Installation:
    echo 1. Copy AdminPlugin.dll to: csgo/addons/counterstrikesharp/plugins/AdminPlugin/
    echo 2. Configure admins in: csgo/addons/counterstrikesharp/configs/admins.json
    echo 3. Restart server or use: css_plugins reload AdminPlugin
    echo.
) else (
    echo.
    echo Build failed!
    echo.
)

pause
