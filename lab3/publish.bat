@echo off
setlocal
cd /d "%~dp0"
dotnet publish "App\Lab3Variant9.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -o "ReadyExe"
if errorlevel 1 (
  echo Publication failed. See the errors above.
  pause
  exit /b 1
)
echo Ready: ReadyExe\Lab3Variant9.exe
pause
