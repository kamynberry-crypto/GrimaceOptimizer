@echo off
setlocal
cd /d "%~dp0"

echo ========================================
echo       GRIMACE OPTIMIZER BUILD
echo ========================================
echo.

dotnet --info >nul 2>&1
if errorlevel 1 (
  echo .NET SDK was not found.
  echo Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
  echo Then close and reopen this window and run build.bat again.
  pause
  exit /b 1
)

dotnet publish GrimaceOptimizer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true
if errorlevel 1 (
  echo.
  echo BUILD FAILED.
  pause
  exit /b 1
)

echo.
echo BUILD COMPLETE.
echo EXE:
echo %~dp0bin\Release\net8.0-windows\win-x64\publish\GrimaceOptimizer.exe
pause
