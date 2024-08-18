using System.Diagnostics;
using System.Text;

namespace pacman_speedster;

public static class ScriptTool
{
    public static string[] Run(string[] scripts)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllLines(tempFile, scripts);
        var proc = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = tempFile,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(proc);
        if (process == null) throw new NullReferenceException();
        using var standardInput = process.StandardInput;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(1000);
        return output.Trim().Split('\n');
    }
    public static string[] Run(string fileName)
    {
        var scripts = File.ReadAllLines(fileName);
        return Run(scripts);
    }

    public static string Write(string text)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, text, Encoding.UTF8);
        return tempFile;
    }

    public static string Write(string[] lines) => Write(string.Join("\n", lines));
}