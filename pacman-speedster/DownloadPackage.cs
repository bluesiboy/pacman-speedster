using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace pacman_speedster;

public static class DownloadPackage
{
    public static List<ServerData> Servers { get; set; }
    public static List<(string, string)> RemainingPackages { get; set; }
    private static object _ListLock { get; } = new();
    public static int MaxErrorCount { get; } = 10;
    public static string TmpPath { get; set; }
    public static IHttpClientFactory HttpClientFactory { get; set; }

    public static void Start()
    {
        TmpPath = Path.GetTempPath() + "pacman_speester";
        Console.WriteLine("Target Path: " + TmpPath);
        Directory.CreateDirectory(TmpPath);
        Console.WriteLine("Downloading package...");
        Servers = InitDataBase.PacmanDatas.SelectMany(x =>
                x.Urls.Select(c => new ServerData { Url = c, Key = x.Key }))
            .ToList();
        var services = new ServiceCollection();
        Servers.ForEach(server =>
            services.AddHttpClient(server.Key + "/" + server.Url, client => client.BaseAddress = new Uri(server.Url.Substring(0, server.Url.IndexOf('/', 9)))));
        var serviceProvider = services.BuildServiceProvider();
        HttpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        RemainingPackages = InitDataBase.Packages.Select(x => (x.Item1, x.Item2)).ToList();
        foreach (var server in Servers)
        {
            server.DownTask = Task.Run(() => DownAsync(TakeOne(server), server));
        }
    }

    public static async Task DownAsync((string, string)? package, ServerData server)
    {
        // Console.WriteLine($"Downloaded {server.Url}");
        if (!package.HasValue || package.Value.Item1 == null) return;
        var targetUrl = $"archlinux/{server.Key}/os/{InitDataBase.OSType}/{package.Value.Item2}";
        server.Working = true;
        try
        {
            var clientName = server.Key + "/" + server.Url;
            // Console.WriteLine(Path.Combine(TmpPath, package.Value.Item2));
            await DownAsync(clientName, targetUrl, Path.Combine(TmpPath, package.Value.Item2));
            await DownAsync(clientName, $"{targetUrl}.sig", Path.Combine(TmpPath, package.Value.Item2 + ".sig"));
            server.SuccessCount++;
        }
        catch (Exception e)
        {
            PutOne((package.Value.Item1, package.Value.Item2));
            await Console.Error.WriteLineAsync($"Failed to download package: {e.Message} \n");
            server.ErrorCount++;
        }
        finally
        {
            server.Working = false;
            if (server.ErrorCount < MaxErrorCount)
            {
                await DownAsync(TakeOne(server), server);
            }
        }
    }

    public static (string, string)? TakeOne(ServerData server)
    {
        lock (_ListLock)
        {
            var package = RemainingPackages.FirstOrDefault(x => x.Item1 == server.Key);
            if (package.Item2 != null)
            {
                RemainingPackages.Remove(package);
            }

            return package;
        }
    }

    public static void PutOne((string, string) package)
    {
        lock (_ListLock)
        {
            RemainingPackages.Add(package);
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

    public static void WaitEnd()
    {
        Console.WriteLine(string.Empty);
        while (true)
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine($"Downloading... {InitDataBase.Packages.Length - RemainingPackages.Count}/{InitDataBase.Packages.Length}");
            if (Servers.Any(server => server.DownTask != null && !server.DownTask.IsCompleted))
            {
                Task.Delay(1000).Wait();
                continue;
            }

            Task.Delay(2000).Wait();

            if (Servers.All(server => server.DownTask == null || server.DownTask.IsCompleted))
            {
                break;
            }
        }
        Console.WriteLine("Program Downloaded.");
    }
}

public class ServerData
{
    public string Url { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int SuccessCount { get; set; } = 0;
    public bool Working { get; set; } = false;
    public int ErrorCount { get; set; }
    public Task? DownTask { get; set; }
}