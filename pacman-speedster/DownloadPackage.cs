using Microsoft.Extensions.DependencyInjection;

namespace pacman_speedster;

public static class DownloadPackage
{
    public static List<ServerData> Servers { get; set; }
    public static List<(string, string)> RemainingPackages { get; set; }
    private static object _TakeOne { get; } = new();
    public static int MaxErrorCount { get; } = 1;
    public static string TmpPath { get; } = Path.GetTempPath();
    public static IHttpClientFactory HttpClientFactory { get; set; }

    public static void Init()
    {
        Console.WriteLine("Target Path: " + TmpPath);
        Console.WriteLine("Downloading package...");
        Servers = InitDataBase.PacmanDatas.SelectMany(x =>
                x.Urls.Select(c => new ServerData { Url = c, Key = x.Key }))
            .ToList();
        var services = new ServiceCollection();
        Servers.ForEach(server =>
            services.AddHttpClient(server.Key + "/" + server.Url, client => client.BaseAddress = new Uri(server.Url)));
        var serviceProvider = services.BuildServiceProvider();
        HttpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        RemainingPackages = InitDataBase.Packages.Select(x => (x.Item1, x.Item2)).ToList();
        foreach (var server in Servers)
        {
            Task.Run(() => DownAsync(TakeOne(server), server));
        }
    }

    public static async Task DownAsync((string, string)? package, ServerData server)
    {
        if (!package.HasValue) return;
        var targetUrl = $"{server.Key}/os/{InitDataBase.OSType}/{package.Value.Item2}";
        server.Working = true;
        try
        {
            var clientName = server.Key + "/" + server.Url;
            await DownAsync(clientName, targetUrl, Path.Combine(TmpPath, package.Value.Item2));
            await DownAsync(clientName, $"{targetUrl}.sig", Path.Combine(TmpPath, package.Value.Item2, ".sig"));
            server.SuccessCount++;
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"Failed to download package: {e.Message}");
            server.ErrorCount++;
        }
        finally
        {
            server.Working = false;
            Console.WriteLine($"UnDownloaded package: {RemainingPackages.Count}");
            if (server.ErrorCount < MaxErrorCount)
            {
                await DownAsync(TakeOne(server), server);
            }
        }
    }

    public static (string, string)? TakeOne(ServerData server)
    {
        lock (_TakeOne)
        {
            var package = RemainingPackages.FirstOrDefault(x => x.Item1 == server.Key);
            if (package.Item2 != null)
            {
                RemainingPackages.Remove(package);
            }

            return package;
        }
    }

    public static async Task DownAsync(string clientName, string url, string destinationFilePath)
    {
        using var client = HttpClientFactory.CreateClient(clientName);
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var fileStream =
            new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream);
    }
}

public class ServerData
{
    public string Url { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int SuccessCount { get; set; } = 0;
    public bool Working { get; set; } = false;
    public int ErrorCount { get; set; }
}