# Grimace Optimizer

A Windows x64 Fortnite-focused optimizer/launcher with a dark purple UI.

## What it does
- Automatically detects CPU, GPU, RAM, and system-drive usage.
- Enables the Windows High Performance power plan.
- Enables Game Mode and disables background Game DVR capture.
- Requests Hardware-Accelerated GPU Scheduling (Windows restart may be required).
- Tunes Windows multimedia game scheduling priority.
- Applies a conservative Fortnite performance preset in `GameUserSettings.ini` while preserving the rest of the file.
- Uses medium texture quality for GPUs like the GTX 1660 Ti 6 GB while lowering expensive effects/shadows.
- Sets Fortnite matchmaking to **Auto**, which Epic recommends for best ping.
- Measures regional network latency and displays an estimate of the closest region.
- Launches Fortnite through the Epic Games Launcher URI and sets Fortnite's process priority to High after launch.
- Deletes only temp files older than two days that are not locked.
- Shows the installed app version and checks GitHub Releases automatically at startup.
- Includes a **Check for Updates** button and can download/install a newer `GrimaceOptimizer.exe` release automatically.

There is intentionally **no backup or restore module** and no Python dependency.

## Enable GitHub updates
1. Create a public GitHub repository for this project, for example `GrimaceOptimizer`.
2. Open `UpdateService.cs`.
3. Replace:
   - `YOUR_GITHUB_USERNAME` with your GitHub username.
   - `GrimaceOptimizer` if your repository has a different name.
4. Commit and push the project.
5. Create releases with tags such as `v1.0.1`, `v1.1.0`, etc.
6. The included GitHub Actions workflow automatically builds a self-contained `GrimaceOptimizer.exe` and attaches it to the release.
7. Installed copies of Grimace will detect the release automatically on startup or when **Check for Updates** is pressed.

The updater looks for a GitHub release asset named exactly `GrimaceOptimizer.exe`.

## Build a standalone EXE
1. On Windows, install the x64 .NET 8 SDK or newer.
2. Double-click `build.bat`.
3. The self-contained EXE will be created at:
   `bin\\Release\\net8.0-windows\\win-x64\\publish\\GrimaceOptimizer.exe`

The published EXE is self-contained and does not need .NET installed on the target PC.

## Release versions
Keep the project version in `GrimaceOptimizer.csproj` aligned with the release tag for local builds. For GitHub releases, the included workflow takes the version from the tag automatically.

## Notes
- The app requests administrator permission because power/GPU scheduling settings are system-level.
- Fortnite should be launched once before optimization so its config file exists.
- Region measurements are estimates from regional network endpoints, not Epic's private game servers. Fortnite itself remains on Auto so Epic can select the live server with the best ping.
- FPS gains vary by map, Fortnite version, background apps, drivers, temperatures, and in-game render mode.
