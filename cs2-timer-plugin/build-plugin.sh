#!/bin/bash
echo "Building CS2 Timer Plugin..."

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
    echo "1. Copy TimerPlugin.dll to: csgo/addons/counterstrikesharp/plugins/TimerPlugin/"
    echo "2. Restart server or use: css_plugins reload TimerPlugin"
    echo ""
else
    echo ""
    echo "Build failed!"
    echo ""
fi
