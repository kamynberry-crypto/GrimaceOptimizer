using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GrimaceOptimizer;

internal sealed record OptimizationOptions(
    bool HighPerformancePower,
    bool GameMode,
    bool DisableGameCapture,
    bool HardwareGpuScheduling,
    bool GamingPriority,
    bool FortniteProfile,
    bool CleanupTempFiles,
    bool DisableXboxOverlay,
    bool ClearShaderCache,
    bool FlushDns,
    bool WindowsVisualEffects,
    bool HighPriorityLaunch);

internal static class Optimizer
{
    public static async Task<List<string>> ApplyAsync(OptimizationOptions options, IProgress<string>? progress = null)
    {
        var done = new List<string>();

        if (options.HighPerformancePower)
        {
            var exit = await RunAsync("powercfg.exe", "/setactive SCHEME_MIN");
            if (exit == 0) Add(done, "High Performance power plan enabled", progress);
            else progress?.Report("High Performance power plan could not be changed");
        }

        if (options.GameMode)
        {
            SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1);
            SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1);
            Add(done, "Windows Game Mode enabled", progress);
        }

        if (options.DisableGameCapture)
        {
            SetDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0);
            SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0);
            Add(done, "Background Game DVR capture disabled", progress);
        }

        if (options.HardwareGpuScheduling)
        {
            try
            {
                SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
                Add(done, "Hardware-accelerated GPU scheduling requested (restart may be needed)", progress);
            }
            catch { progress?.Report("GPU scheduling setting skipped by Windows"); }
        }

        if (options.GamingPriority)
        {
            try
            {
                using var games = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", true);
                games?.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                games?.SetValue("Priority", 6, RegistryValueKind.DWord);
                games?.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                games?.SetValue("SFIO Priority", "High", RegistryValueKind.String);
                Add(done, "Windows multimedia gaming priority tuned", progress);
            }
            catch { progress?.Report("Multimedia priority tuning skipped"); }
        }

        if (options.FortniteProfile) OptimizeFortniteConfig(progress, done);
        if (options.CleanupTempFiles) CleanupTemp(progress, done);
        if (options.DisableXboxOverlay) DisableXboxOverlay(progress, done);
        if (options.ClearShaderCache) ClearShaderCache(progress, done);
        if (options.FlushDns)
        {
            var exit = await RunAsync("ipconfig.exe", "/flushdns");
            if (exit == 0) Add(done, "DNS cache flushed", progress);
            else progress?.Report("DNS cache could not be flushed");
        }
        if (options.WindowsVisualEffects) OptimizeVisualEffects(progress, done);

        return done;
    }

    public static void SetFortniteAutoRegion(IProgress<string>? progress = null)
    {
        string path = GetFortniteConfigPath();
        if (!File.Exists(path))
        {
            progress?.Report("Fortnite config not found yet — launch Fortnite once first");
            return;
        }
        MakeWritable(path);
        string text = File.ReadAllText(path);
        text = SetIniValue(text, "PreferredRegion", "Auto");
        File.WriteAllText(path, text);
        progress?.Report("Fortnite matchmaking region set to Auto (Epic selects best ping)");
    }

    public static void LaunchFortnite(bool boostPriority = true)
    {
        const string url = "com.epicgames.launcher://apps/fn%3A4fe75bbc5a674f4f9b356b5c90567da5%3AFortnite?action=launch&silent=true";
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { Process.Start(new ProcessStartInfo("explorer.exe", $"\"{url}\"") { UseShellExecute = true }); }
        if (boostPriority) _ = BoostFortniteProcessWhenReadyAsync();
    }

    private static void OptimizeFortniteConfig(IProgress<string>? progress, List<string> done)
    {
        string path = GetFortniteConfigPath();
        if (!File.Exists(path)) { progress?.Report("Fortnite config not found — game-specific settings skipped"); return; }
        MakeWritable(path);
        string text = File.ReadAllText(path);
        var settings = new Dictionary<string, string>
        {
            ["bUseVSync"] = "False",
            ["bUseDynamicResolution"] = "False",
            ["FrameRateLimit"] = "0.000000",
            ["sg.AntiAliasingQuality"] = "0",
            ["sg.ShadowQuality"] = "0",
            ["sg.GlobalIlluminationQuality"] = "0",
            ["sg.ReflectionQuality"] = "0",
            ["sg.PostProcessQuality"] = "0",
            ["sg.TextureQuality"] = "1",
            ["sg.EffectsQuality"] = "0",
            ["sg.FoliageQuality"] = "0",
            ["sg.ShadingQuality"] = "0",
            ["PreferredRegion"] = "Auto"
        };
        foreach (var item in settings) text = SetIniValue(text, item.Key, item.Value);
        File.WriteAllText(path, text);
        Add(done, "Fortnite performance profile applied (medium textures for 6 GB VRAM, low effects/shadows, VSync off)", progress);
    }

    private static void DisableXboxOverlay(IProgress<string>? progress, List<string> done)
    {
        try
        {
            SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0);
            SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "ShowStartupPanel", 0);
            Add(done, "Xbox Game Bar overlay startup disabled", progress);
        }
        catch { progress?.Report("Xbox Game Bar overlay setting skipped"); }
    }

    private static void ClearShaderCache(IProgress<string>? progress, List<string> done)
    {
        int removed = 0;
        var paths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NVIDIA", "DXCache")
        };
        foreach (var dir in paths)
        {
            try
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.EnumerateFiles(dir).Take(1500))
                {
                    try { File.Delete(file); removed++; } catch { }
                }
            }
            catch { }
        }
        Add(done, $"Graphics shader cache cleanup completed ({removed} files removed where available)", progress);
    }

    private static void OptimizeVisualEffects(IProgress<string>? progress, List<string> done)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
            key?.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord);
            Add(done, "Windows visual-effects profile set for performance", progress);
        }
        catch { progress?.Report("Windows visual-effects setting skipped"); }
    }

    private static void CleanupTemp(IProgress<string>? progress, List<string> done)
    {
        string temp = Path.GetTempPath(); int removed = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(temp).Take(1200))
            {
                try { var info = new FileInfo(file); if (info.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-2)) { info.Delete(); removed++; } } catch { }
            }
        }
        catch { }
        Add(done, $"Temporary file cleanup completed ({removed} old files removed)", progress);
    }

    private static async Task BoostFortniteProcessWhenReadyAsync()
    {
        for (int i = 0; i < 60; i++)
        {
            await Task.Delay(2000);
            try
            {
                var procs = Process.GetProcesses().Where(p => p.ProcessName.Contains("FortniteClient-Win64-Shipping", StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var p in procs) { try { p.PriorityClass = ProcessPriorityClass.High; } catch { } }
                if (procs.Count > 0) return;
            }
            catch { }
        }
    }

    private static string GetFortniteConfigPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteGame", "Saved", "Config", "WindowsClient", "GameUserSettings.ini");

    private static string SetIniValue(string text, string key, string value)
    {
        string pattern = $@"(?m)^\s*{Regex.Escape(key)}\s*=.*$";
        string replacement = $"{key}={value}";
        if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase)) return Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase);
        return text.TrimEnd() + Environment.NewLine + replacement + Environment.NewLine;
    }

    private static void MakeWritable(string path) { try { File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly); } catch { } }
    private static void SetDword(RegistryKey hive, string subkey, string name, int value) { using var key = hive.CreateSubKey(subkey, true); key?.SetValue(name, value, RegistryValueKind.DWord); }
    private static async Task<int> RunAsync(string file, string args)
    {
        try { var psi = new ProcessStartInfo(file, args) { UseShellExecute = false, CreateNoWindow = true }; using var p = Process.Start(psi)!; await p.WaitForExitAsync(); return p.ExitCode; }
        catch { return -1; }
    }
    private static void Add(List<string> done, string message, IProgress<string>? progress) { done.Add(message); progress?.Report(message); }
}
