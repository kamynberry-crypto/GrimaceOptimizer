using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace GrimaceOptimizer;

internal sealed record HardwareSnapshot(string Cpu, string Gpu, string Ram, string Storage, string WindowsVersion);

internal static class HardwareInfo
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    public static async Task<HardwareSnapshot> DetectAsync()
    {
        string cpu = ReadCpu();
        string gpu = await ReadGpuAsync();
        string ram = ReadRam();
        string storage = ReadStorage();
        string windows = Environment.OSVersion.VersionString;
        return new(cpu, gpu, ram, storage, windows);
    }

    private static string ReadCpu()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return (key?.GetValue("ProcessorNameString")?.ToString() ?? "Unknown CPU").Trim();
        }
        catch { return "Unknown CPU"; }
    }

    private static string ReadRam()
    {
        try
        {
            var status = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(status))
                return $"{status.ullTotalPhys / 1024d / 1024d / 1024d:0.0} GB";
        }
        catch { }
        return "Unknown RAM";
    }

    private static string ReadStorage()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!);
            double total = drive.TotalSize / 1024d / 1024d / 1024d;
            double used = (drive.TotalSize - drive.AvailableFreeSpace) / 1024d / 1024d / 1024d;
            return $"{used:0} GB used / {total:0} GB system drive";
        }
        catch { return "Unknown storage"; }
    }

    private static async Task<string> ReadGpuAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-CimInstance Win32_VideoController | Select-Object -First 1 Name,AdapterRAM | ConvertTo-Json -Compress\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi)!;
            string json = await p.StandardOutput.ReadToEndAsync();
            await p.WaitForExitAsync();
            if (!string.IsNullOrWhiteSpace(json))
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string name = root.TryGetProperty("Name", out var n) ? n.GetString() ?? "Unknown GPU" : "Unknown GPU";
                long bytes = root.TryGetProperty("AdapterRAM", out var r) && r.ValueKind == JsonValueKind.Number ? r.GetInt64() : 0;
                return bytes > 0 ? $"{name} ({bytes / 1024d / 1024d / 1024d:0.#} GB)" : name;
            }
        }
        catch { }
        return "Unknown GPU";
    }
}
