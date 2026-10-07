using System.Management;

namespace GrimaceOptimizer;

public sealed record HardwareInfo(string Cpu, string Gpu, string Ram, string Storage)
{
    public static Task<HardwareInfo> DetectAsync()
    {
        string cpu = "Detecting...";
        string gpu = "Detecting...";
        string ram = "Detecting...";
        string storage = "Detecting...";

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            cpu = searcher.Get().Cast<ManagementObject>().FirstOrDefault()?["Name"]?.ToString() ?? "Unknown CPU";
        } catch { cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown CPU"; }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            gpu = searcher.Get().Cast<ManagementObject>().Select(x => x["Name"]?.ToString())
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "Unknown GPU";
        } catch { gpu = "Unknown GPU"; }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            var kb = Convert.ToDouble(searcher.Get().Cast<ManagementObject>().First()["TotalVisibleMemorySize"]);
            ram = $"{kb / 1024 / 1024:0.0} GB RAM";
        } catch { ram = "Unknown RAM"; }

        try
        {
            var root = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!);
            storage = $"{root.TotalSize / 1e9:0} GB total / {root.AvailableFreeSpace / 1e9:0} GB free";
        } catch { storage = "Unknown storage"; }

        return Task.FromResult(new HardwareInfo(cpu, gpu, ram, storage));
    }
}
