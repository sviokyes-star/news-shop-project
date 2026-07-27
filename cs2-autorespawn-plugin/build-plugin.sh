#!/bin/bash
echo "Building CS2 Auto Respawn Plugin..."

dotnet build -c Release

if [ $? -eq 0 ]; then
    echo ""
    echo "================================"
    echo "Build successful!"
    echo "================================"
    echo ""
    echo "Plugin files are in: bin/Release/net8.0/"
    echo ""
    echo "Installation:"
    echo "1. Copy AutoRespawnPlugin.dll to: csgo/addons/counterstrikesharp/plugins/AutoRespawnPlugin/"
    echo "2. Restart server or use: css_plugins reload AutoRespawnPlugin"
    echo ""
else
    echo ""
    echo "Build failed!"
    echo ""
fi
