using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace pacman_speedster;

public static class InitDataBase
{
    public static List<PacmanData> PacmanDatas { get; set; } = new();
    public static (string, string)[] Packages { get; set; } = [];

    public static string OSType { get; } = RuntimeInformation.OSArchitecture switch
    {
        Architecture.X64 => "x86_64",
        _ => throw new ArgumentOutOfRangeException()
    };

    public static void LoadPacmanData()
    {
        Console.Write("Loading pacman data...");
        // var result = ScriptTool.Run("./scripts/get-pacman.sh");
        var result = File.ReadAllLines("/etc/pacman.conf");
        result = result.Where(x => x.Contains('=') || x.StartsWith('[')).ToArray();
        var pacFile = ScriptTool.Write(result);
        var config = new ConfigurationBuilder()
            .AddIniFile(pacFile)
            .SetFileLoadExceptionHandler(ex => ex.Ignore = true)
            .Build();
        var sections = config.GetChildren()
            .Where(x => x["Server"] != null || x["Include"] != null);
        foreach (var section in sections)
        {
            var item = new PacmanData
            {
                Key = section.Key,
                Value = section["Server"]?.Trim() ?? section["Include"]?.Trim()
            };
            item.Urls = item.Value.StartsWith("http")
                ? [item.Value]
                : File.ReadAllLines(item.Value)
                    .Where(x => x.Contains('=') && x.StartsWith("Server"))
                    .Select(x => x.Split('=')[1].Trim())
                    .ToArray();
            PacmanDatas.Add(item);
        }

        // Task.Delay(1000).Wait();
        Console.WriteLine($" {PacmanDatas.Count} pacman data");
    }

    public static void AnalysePacmanData()
    {
        Console.Write("Analysing package data...");
        var urlPart = $"/os/{OSType}/";
        Packages = ScriptTool.Run(["pacman -Sup"])
            // .Where(x => x.StartsWith("http") || x.StartsWith("file"))
            .Where(x => x.StartsWith("http"))
            .Select(x =>
            {
                var a = x.Split(urlPart);
                return (a[0][(a[0].LastIndexOf('/') + 1)..], a[1]);
            })
            .ToArray();
        Console.WriteLine($" {Packages.Length} package data");
    }

    public static void PacmanSy()
    {
        Console.Write("pacman -Sy");
        ScriptTool.Run(["pacman -Sy"]);
    }
}

public class PacmanData
{
    public string Key { get; set; }
    public string Value { get; set; }
    public string[] Urls { get; set; }
}