using System.Diagnostics;
using System.Net.Sockets;

namespace GrimaceOptimizer;

internal sealed record RegionResult(string Name, string Code, int Milliseconds);

internal static class RegionProbe
{
    private static readonly (string Name, string Code, string Host)[] Targets =
    {
        ("NA-East", "NAE", "s3.us-east-1.amazonaws.com"),
        ("NA-Central", "NAC", "s3.us-east-2.amazonaws.com"),
        ("NA-West", "NAW", "s3.us-west-2.amazonaws.com"),
        ("Europe", "EU", "s3.eu-west-1.amazonaws.com"),
        ("Brazil", "BR", "s3.sa-east-1.amazonaws.com"),
        ("Asia", "ASIA", "s3.ap-northeast-1.amazonaws.com"),
        ("Oceania", "OCE", "s3.ap-southeast-2.amazonaws.com"),
        ("Middle East", "ME", "s3.me-central-1.amazonaws.com")
    };

    public static async Task<RegionResult?> FindBestAsync(IProgress<string>? progress = null)
    {
        var tasks = Targets.Select(t => ProbeAsync(t.Name, t.Code, t.Host, progress));
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).OrderBy(r => r!.Milliseconds).FirstOrDefault();
    }

    private static async Task<RegionResult?> ProbeAsync(string name, string code, string host, IProgress<string>? progress)
    {
        var samples = new List<long>();
        for (int i = 0; i < 2; i++)
        {
            try
            {
                using var client = new TcpClient();
                var sw = Stopwatch.StartNew();
                using var cts = new CancellationTokenSource(1600);
                await client.ConnectAsync(host, 443, cts.Token);
                sw.Stop();
                samples.Add(sw.ElapsedMilliseconds);
            }
            catch { }
        }
        if (samples.Count == 0) return null;
        int ms = (int)Math.Round(samples.Average());
        progress?.Report($"{name}: ~{ms} ms");
        return new RegionResult(name, code, ms);
    }
}
