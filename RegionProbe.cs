using System.Net;
using System.Net.Http;

namespace GrimaceOptimizer;

public static class RegionProbe
{
    public static async Task<string> FindBestAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var regions = new[]
            {
                ("NA East", "https://ec2.us-east-1.amazonaws.com"),
                ("NA Central", "https://ec2.us-east-2.amazonaws.com"),
                ("NA West", "https://ec2.us-west-1.amazonaws.com")
            };

            var results = new List<(string Name, long Ms)>();
            foreach (var region in regions)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    using var r = await client.GetAsync(region.Item2);
                    sw.Stop();
                    results.Add((region.Item1, sw.ElapsedMilliseconds));
                }
                catch { }
            }
            return results.OrderBy(x => x.Ms).FirstOrDefault().Name ?? "Auto";
        }
        catch { return "Auto"; }
    }
}
