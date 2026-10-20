@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo Install .NET SDK 10 and run this file again
    echo https://dotnet.microsoft.com/download/dotnet/10.0
    pause
    exit /b 1
)

dotnet publish "RasterLab\RasterLab.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -o "%~dp0publish\win-x64"
if errorlevel 1 (
    echo Publish failed
    pause
    exit /b 1
)

echo.
echo Ready: "%~dp0publish\win-x64\RasterLab.exe"
start "" "%~dp0publish\win-x64"
pause
