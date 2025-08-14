#!/bin/bash

# Build script for IIM Plugin System

set -e

echo "Building IIM Plugin System..."

# Build SDK
echo "Building Plugin SDK..."
dotnet build src/IIM.Plugin.SDK/IIM.Plugin.SDK.csproj -c Release
dotnet pack src/IIM.Plugin.SDK/IIM.Plugin.SDK.csproj -c Release -o ./packages

# Build CLI
echo "Building IIM CLI..."
dotnet build tools/IIM.CLI/IIM.CLI.csproj -c Release
dotnet pack tools/IIM.CLI/IIM.CLI.csproj -c Release -o ./packages

# Build sample plugin
echo "Building Sample Plugin..."
dotnet build examples/SamplePlugin/SamplePlugin.csproj -c Release

# Package sample plugin
cd examples/SamplePlugin
../../tools/IIM.CLI/bin/Release/net8.0/IIM.CLI plugin package
cd ../..

echo "Plugin system build complete!"
echo "Packages created in ./packages/"
echo "Sample plugin created in examples/SamplePlugin/bin/Release/"
