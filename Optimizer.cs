using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GrimaceOptimizer;

internal static class Optimizer
{
    public static async Task<List<string>> ApplyAsync(IProgress<string>? progress = null)
    {
        var done = new List<string>();

        await RunAsync("powercfg.exe", "/setactive SCHEME_MIN");
        done.Add("High Performance power plan enabled");
        progress?.Report(done[^1]);

        SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1);
        SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1);
        done.Add("Windows Game Mode enabled");
        progress?.Report(done[^1]);

        SetDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0);
        SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0);
        done.Add("Background Game DVR capture disabled");
        progress?.Report(done[^1]);

        try
        {
            SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
            done.Add("Hardware-accelerated GPU scheduling requested (restart may be needed)");
            progress?.Report(done[^1]);
        }
        catch
        {
            progress?.Report("GPU scheduling setting skipped by Windows");
        }

        try
        {
            using var games = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", true);
            games?.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
            games?.SetValue("Priority", 6, RegistryValueKind.DWord);
            games?.SetValue("Scheduling Category", "High", RegistryValueKind.String);
            games?.SetValue("SFIO Priority", "High", RegistryValueKind.String);
            done.Add("Windows multimedia gaming priority tuned");
            progress?.Report(done[^1]);
        }
        catch { progress?.Report("Multimedia priority tuning skipped"); }

        OptimizeFortniteConfig(progress, done);
        CleanupTemp(progress, done);
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

    public static void LaunchFortnite()
    {
        const string url = "com.epicgames.launcher://apps/fn%3A4fe75bbc5a674f4f9b356b5c90567da5%3AFortnite?action=launch&silent=true";
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{url}\"") { UseShellExecute = true });
        }
        _ = BoostFortniteProcessWhenReadyAsync();
    }

    private static void OptimizeFortniteConfig(IProgress<string>? progress, List<string> done)
    {
        string path = GetFortniteConfigPath();
        if (!File.Exists(path))
        {
            progress?.Report("Fortnite config not found — game-specific settings skipped");
            return;
        }

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
        foreach (var item in settings)
            text = SetIniValue(text, item.Key, item.Value);
        File.WriteAllText(path, text);
        done.Add("Fortnite performance profile applied (medium textures for 6 GB VRAM, low effects/shadows, VSync off)");
        progress?.Report(done[^1]);
    }

    private static void CleanupTemp(IProgress<string>? progress, List<string> done)
    {
        string temp = Path.GetTempPath();
        int removed = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(temp).Take(1200))
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-2)) { info.Delete(); removed++; }
                }
                catch { }
            }
        }
        catch { }
        done.Add($"Temporary file cleanup completed ({removed} old files removed)");
        progress?.Report(done[^1]);
    }

    private static async Task BoostFortniteProcessWhenReadyAsync()
    {
        for (int i = 0; i < 60; i++)
        {
            await Task.Delay(2000);
            try
            {
                var procs = Process.GetProcesses().Where(p => p.ProcessName.Contains("FortniteClient-Win64-Shipping", StringComparison.OrdinalIgnoreCase));
                foreach (var p in procs)
                {
                    try { p.PriorityClass = ProcessPriorityClass.High; } catch { }
                }
                if (procs.Any()) return;
            }
            catch { }
        }
    }

    private static string GetFortniteConfigPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FortniteGame", "Saved", "Config", "WindowsClient", "GameUserSettings.ini");

    private static string SetIniValue(string text, string key, string value)
    {
        string pattern = $@"(?m)^\s*{Regex.Escape(key)}\s*=.*$";
        string replacement = $"{key}={value}";
        if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            return Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase);
        return text.TrimEnd() + Environment.NewLine + replacement + Environment.NewLine;
    }

    private static void MakeWritable(string path)
    {
        try { File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly); } catch { }
    }

    private static void SetDword(RegistryKey hive, string subkey, string name, int value)
    {
        using var key = hive.CreateSubKey(subkey, true);
        key?.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static async Task<int> RunAsync(string file, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(file, args) { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi)!;
            await p.WaitForExitAsync();
            return p.ExitCode;
        }
        catch { return -1; }
    }
}
