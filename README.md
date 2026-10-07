# Grimace Optimizer v1.3.0

Native Windows Forms Fortnite optimizer for Windows 10/11 x64.

## Included
- Automatic CPU/GPU/RAM/storage detection
- Estimated low-ping region probe while keeping Fortnite matchmaking on Auto
- SYSTEM, WINDOWS GAMING, FORTNITE, NETWORK, CLEANUP and LAUNCH sections
- FPS cap selection
- Fortnite competitive settings profile
- Windows gaming tweaks
- DNS flush and optional shader-cache cleanup
- Fortnite launch button
- GitHub update checker with a separate PowerShell updater process
- Version displayed in the app

## Build
Requires the .NET 8 SDK.

Run `build.bat`. The published EXE will be in `publish\GrimaceOptimizer.exe`.

## Release
Upload the exact file `GrimaceOptimizer.exe` as the release asset. The updater looks for that exact asset name in the latest GitHub release.

Repository: https://github.com/kamynberry-crypto/GrimaceOptimizer

## Notes
This is an unsigned Windows executable, so Microsoft Defender SmartScreen may warn on first run. Only run binaries downloaded from your own GitHub release.

Some Windows and Fortnite settings are version-dependent and may require a restart. No FPS result is guaranteed.
