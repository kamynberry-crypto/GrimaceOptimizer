using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace GrimaceOptimizer;

internal sealed record UpdateInfo(Version Version, string TagName, string DownloadUrl, string ReleaseUrl, string? Notes);

internal static class UpdateService
{
    // Change these two values to the GitHub account/repository that publishes Grimace releases.
    // Example: GitHub owner = "Berry123", repository = "GrimaceOptimizer".
    private const string GitHubOwner = "kamynberry-crypto";
    private const string GitHubRepository = "GrimaceOptimizer";

    private static readonly HttpClient Client = CreateClient();

    public static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
    public static string VersionText => $"v{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";
    public static bool IsConfigured => !GitHubOwner.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GrimaceOptimizer", VersionTextForHeader()));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.Timeout = TimeSpan.FromSeconds(15);
        return client;
    }

    private static string VersionTextForHeader()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        return $"{v.Major}.{v.Minor}.{Math.Max(v.Build, 0)}";
    }

    public static async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return null;

        var endpoint = $"https://api.github.com/repos/{Uri.EscapeDataString(GitHubOwner)}/{Uri.EscapeDataString(GitHubRepository)}/releases/latest";
        using var response = await Client.GetAsync(endpoint, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;

        if (root.TryGetProperty("draft", out var draft) && draft.GetBoolean())
            return null;
        if (root.TryGetProperty("prerelease", out var prerelease) && prerelease.GetBoolean())
            return null;

        var tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
        var version = ParseVersion(tag);
        if (version is null || version <= CurrentVersion)
            return null;

        string? downloadUrl = null;
        if (root.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (name.Equals("GrimaceOptimizer.exe", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(downloadUrl))
            return null;

        var releaseUrl = root.TryGetProperty("html_url", out var html) ? html.GetString() ?? string.Empty : string.Empty;
        var notes = root.TryGetProperty("body", out var body) ? body.GetString() : null;
        return new UpdateInfo(version, tag, downloadUrl!, releaseUrl, notes);
    }

    public static async Task<string> DownloadAndStageAsync(UpdateInfo update, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GrimaceOptimizerUpdate");
        Directory.CreateDirectory(tempDir);
        var newExe = Path.Combine(tempDir, $"GrimaceOptimizer-{update.Version}.exe");

        progress?.Report("Downloading the latest Grimace Optimizer…");
        using var response = await Client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(newExe);
        await input.CopyToAsync(output, cancellationToken);
        await output.FlushAsync(cancellationToken);

        var currentExe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExe))
            throw new InvalidOperationException("The current executable path could not be determined.");

        var updaterScript = Path.Combine(tempDir, "apply-update.cmd");
        var pid = Environment.ProcessId;
        var currentDir = Path.GetDirectoryName(currentExe) ?? AppContext.BaseDirectory;
        var backupName = Path.Combine(tempDir, "GrimaceOptimizer-old.exe");

        var script = $"""
@echo off
setlocal
set PID={pid}
set CURRENT="{currentExe}"
set NEW="{newExe}"
set BACKUP="{backupName}"
:wait
 tasklist /FI "PID eq %PID%" 2>NUL | find /I "%PID%" >NUL
 if not errorlevel 1 (
   timeout /t 1 /nobreak >NUL
   goto wait
 )
 move /Y %CURRENT% %BACKUP% >NUL
 move /Y %NEW% %CURRENT% >NUL
 start "" %CURRENT%
 del /Q %BACKUP% >NUL 2>&1
 del "%~f0"
""";
        await File.WriteAllTextAsync(updaterScript, script, cancellationToken);

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{updaterScript}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = currentDir
        });

        return newExe;
    }

    public static Process? OpenReleasePage(UpdateInfo update)
    {
        if (string.IsNullOrWhiteSpace(update.ReleaseUrl)) return null;
        return Process.Start(new ProcessStartInfo(update.ReleaseUrl) { UseShellExecute = true });
    }

    private static Version? ParseVersion(string tag)
    {
        var cleaned = tag.Trim().TrimStart('v', 'V');
        var dash = cleaned.IndexOf('-');
        if (dash >= 0) cleaned = cleaned[..dash];
        return Version.TryParse(cleaned, out var version) ? version : null;
    }
}
