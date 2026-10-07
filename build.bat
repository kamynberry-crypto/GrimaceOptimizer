@echo off
setlocal
dotnet restore
if errorlevel 1 exit /b 1
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o publish
if errorlevel 1 exit /b 1
echo.
echo Build complete: publish\GrimaceOptimizer.exe
