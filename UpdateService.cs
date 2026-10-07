using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GrimaceOptimizer;

public static class UpdateService
{
    public const string VersionText = "1.3.0";
    private const string Owner = "kamynberry-crypto";
    private const string Repo = "GrimaceOptimizer";
    private const string AssetName = "GrimaceOptimizer.exe";

    public sealed record UpdateInfo(string Version, string DownloadUrl);

    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient();
        c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GrimaceOptimizer", VersionText));
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return c;
    }

    public static async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            using var response = await Client.GetAsync(
                $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);

            var tag = json.RootElement.GetProperty("tag_name").GetString() ?? "";
            var version = tag.TrimStart('v', 'V');
            if (!Version.TryParse(version, out var remote) ||
                !Version.TryParse(VersionText, out var current) ||
                remote <= current)
                return null;

            foreach (var asset in json.RootElement.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                if (string.Equals(name, AssetName, StringComparison.OrdinalIgnoreCase))
                {
                    return new UpdateInfo(version, asset.GetProperty("browser_download_url").GetString()!);
                }
            }
        }
        catch
        {
            // Updating must never prevent the optimizer from starting.
        }
        return null;
    }

    public static async Task<bool> StartUpdateAsync(UpdateInfo info)
    {
        try
        {
            var currentExe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(currentExe) || !File.Exists(currentExe))
                return false;

            var tempExe = Path.Combine(Path.GetTempPath(),
                $"GrimaceOptimizer-update-{Guid.NewGuid():N}.exe");

            using (var response = await Client.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var input = await response.Content.ReadAsStreamAsync();
                await using var output = File.Create(tempExe);
                await input.CopyToAsync(output);
            }

            var tempUpdater = Path.Combine(Path.GetTempPath(),
                $"GrimaceOptimizer-updater-{Guid.NewGuid():N}.ps1");

            var script = $@"
$ErrorActionPreference = 'Stop'
$pidToWait = {Environment.ProcessId}
$oldExe = '{EscapePs(currentExe)}'
$newExe = '{EscapePs(tempExe)}'
try {{
    while (Get-Process -Id $pidToWait -ErrorAction SilentlyContinue) {{ Start-Sleep -Milliseconds 250 }}
    Start-Sleep -Milliseconds 500
    for ($i = 0; $i -lt 20; $i++) {{
        try {{
            Copy-Item -LiteralPath $newExe -Destination $oldExe -Force
            break
        }} catch {{ Start-Sleep -Milliseconds 500 }}
    }}
    if (-not (Test-Path -LiteralPath $oldExe)) {{ throw 'Updated EXE was not installed.' }}
    Remove-Item -LiteralPath $newExe -Force -ErrorAction SilentlyContinue
    Start-Process -FilePath $oldExe
}} catch {{
    # Leave the downloaded file for troubleshooting rather than deleting the working app.
}}
Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue
";

            await File.WriteAllTextAsync(tempUpdater, script);
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "{tempUpdater}"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string EscapePs(string value) => value.Replace("'", "''");
}
