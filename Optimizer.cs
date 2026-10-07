using Microsoft.Win32;
using System.Diagnostics;

namespace GrimaceOptimizer;

public sealed record OptimizationOptions(
    bool HighPerformancePower,
    bool DisablePowerThrottling,
    bool Hags,
    bool GamingPriority,
    bool VisualPerformance,
    bool DisableAnimations,
    bool GameMode,
    bool DisableGameDvr,
    bool DisableXboxOverlay,
    bool CompetitiveProfile,
    bool Fullscreen,
    bool NvidiaReflex,
    bool AutoRegion,
    int FrameRateLimit,
    bool FlushDns,
    bool CleanTemp,
    bool ClearShaderCache,
    bool HighPriority);

public static class Optimizer
{
    public static async Task ApplyAsync(OptimizationOptions o, IProgress<string>? progress = null)
    {
        void P(string s) => progress?.Report(s);
        if (o.HighPerformancePower) { P("Setting High Performance power plan..."); Run("powercfg.exe", "/setactive SCHEME_MIN"); }
        if (o.DisablePowerThrottling) { P("Disabling Windows power throttling..."); SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1); }
        if (o.Hags) { P("Requesting Hardware-accelerated GPU scheduling..."); SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2); }
        if (o.GamingPriority) { P("Applying gaming priority settings..."); SetGamePriority(); }
        if (o.VisualPerformance) { P("Reducing Windows visual effects..."); SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2); }
        if (o.DisableAnimations) { P("Disabling window animations..."); SetString(Registry.CurrentUser, @"Control Panel\Desktop\WindowMetrics", "MinAnimate", "0"); }
        if (o.GameMode) { P("Enabling Windows Game Mode..."); SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1); SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1); }
        if (o.DisableGameDvr) { P("Disabling Game DVR capture..."); SetDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0); SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0); }
        if (o.DisableXboxOverlay) { P("Reducing Xbox Game Bar startup behavior..."); SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0); SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "ShowStartupPanel", 0); }

        P("Updating Fortnite settings...");
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini");
        if (o.CompetitiveProfile) WriteIni(path, "bUseVSync", "False", "bUseDynamicResolution", "False", "sg.AntiAliasingQuality", "0", "sg.ShadowQuality", "0", "sg.GlobalIlluminationQuality", "0", "sg.ReflectionQuality", "0", "sg.PostProcessQuality", "0", "sg.TextureQuality", "1", "sg.EffectsQuality", "0", "sg.FoliageQuality", "0", "sg.ShadingQuality", "0", "sg.ResolutionQuality", "100.000000");
        if (o.Fullscreen) WriteIni(path, "FullscreenMode", "0", "LastConfirmedFullscreenMode", "0", "PreferredFullscreenMode", "0");
        if (o.NvidiaReflex) WriteIni(path, "bEnableNvidiaReflex", "True", "NvidiaReflexMode", "1");
        if (o.FrameRateLimit > 0) WriteIni(path, "FrameRateLimit", o.FrameRateLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (o.AutoRegion) WriteIni(path, "PreferredRegion", "Auto");

        if (o.FlushDns) { P("Flushing DNS cache..."); Run("ipconfig.exe", "/flushdns"); }
        if (o.CleanTemp) { P("Cleaning old temporary files..."); CleanTemp(); }
        if (o.ClearShaderCache) { P("Clearing shader cache files..."); ClearDir(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache")); ClearDir(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NVIDIA\DXCache")); }

        P("Optimization complete.");
        await Task.CompletedTask;
    }

    public static void LaunchFortnite()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "com.epicgames.launcher://apps/fn%3A4fe75bbc5a674f4f9b356b5c90567da5%3AFortnite?action=launch&silent=true",
                UseShellExecute = true
            });
        }
        catch { }
        _ = Task.Run(async () =>
        {
            await Task.Delay(7000);
            foreach (var p in Process.GetProcessesByName("FortniteClient-Win64-Shipping"))
            {
                try { p.PriorityClass = ProcessPriorityClass.High; } catch { }
            }
        });
    }

    static void SetGamePriority()
    {
        const string path = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
        SetDword(Registry.LocalMachine, path, "GPU Priority", 8);
        SetDword(Registry.LocalMachine, path, "Priority", 6);
        SetString(Registry.LocalMachine, path, "Scheduling Category", "High");
        SetString(Registry.LocalMachine, path, "SFIO Priority", "High");
    }

    static void WriteIni(string path, params string[] pairs)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();
        for (int i = 0; i + 1 < pairs.Length; i += 2)
        {
            var key = pairs[i]; var value = pairs[i + 1];
            var idx = lines.FindIndex(x => x.TrimStart().StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) lines[idx] = key + "=" + value;
            else lines.Add(key + "=" + value);
        }
        File.WriteAllLines(path, lines);
    }

    static void CleanTemp()
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-2);
            var dirs = new[] { Path.GetTempPath(), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp") };
            int count = 0;
            foreach (var dir in dirs.Distinct())
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    if (count >= 1200) break;
                    try { if (File.GetLastWriteTime(file) < cutoff) { File.Delete(file); count++; } } catch { }
                }
            }
        } catch { }
    }

    static void ClearDir(string dir)
    {
        try
        {
            if (!Directory.Exists(dir)) return;
            foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                try { File.Delete(f); } catch { }
        } catch { }
    }

    static void Run(string exe, string args)
    {
        try { using var p = Process.Start(new ProcessStartInfo(exe, args) { CreateNoWindow = true, UseShellExecute = false }); p?.WaitForExit(5000); } catch { }
    }
    static void SetDword(RegistryKey root, string path, string name, int value)
    {
        try { using var key = root.CreateSubKey(path); if (key != null) key.SetValue(name, value, RegistryValueKind.DWord); } catch { }
    }
    static void SetString(RegistryKey root, string path, string name, string value)
    {
        try { using var key = root.CreateSubKey(path); if (key != null) key.SetValue(name, value, RegistryValueKind.String); } catch { }
    }
}
