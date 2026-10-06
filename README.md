# Grimace Optimizer v1.2.0

Native Windows x64 Fortnite-focused optimizer and launcher with the same dark Grimace UI. No Python and no backup/restore module.

## v1.2.0 options
- High Performance power plan
- Windows Game Mode
- Disable background Game DVR capture
- Hardware-Accelerated GPU Scheduling request
- Windows multimedia gaming priority tuning
- Fortnite FPS profile (low effects/shadows, medium textures for 6 GB VRAM, VSync off, matchmaking Auto)
- Old temporary-file cleanup
- High-priority Fortnite launch
- Disable Xbox Game Bar overlay startup
- Clear DirectX/NVIDIA shader cache (optional; caches rebuild afterward)
- Flush DNS cache
- Windows visual-effects performance profile

The last four are optional and disabled by default because they can have tradeoffs. The app does not claim a guaranteed FPS increase.

## Hardware
Automatically detects CPU, GPU, RAM, system-drive usage, and Windows version. The UI is tuned for the supplied Ryzen 5 3600 + GTX 1660 Ti 6 GB + 16 GB RAM hardware, but the detector works on other Windows PCs too.

## Region
The app measures regional network endpoints and displays an estimate. Fortnite remains on **Auto** so Epic can select the live server with the best ping; the probe is not a guarantee of in-game ping.

## Updates
The updater is configured for `kamynberry-crypto/GrimaceOptimizer` and checks GitHub Releases at startup or through **Check for Updates**. Releases must include an asset named exactly `GrimaceOptimizer.exe`.

For a new version, update the four version fields in `GrimaceOptimizer.csproj`, commit the changes, and create a GitHub release/tag such as `v1.2.0`. The included Actions workflow builds a self-contained Windows x64 EXE and attaches it to the release.

## Build
Install the .NET 8 SDK on Windows, then run `build.bat`. The standalone EXE is created at:
`bin\\Release\\net8.0-windows\\win-x64\\publish\\GrimaceOptimizer.exe`
