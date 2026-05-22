static class Dotnet
{
    public static async Task<(int ExitCode, string Output)> RunAsync(string arguments, bool echo = false)
    {
        var info = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(info)!;
        var output = new StringBuilder();

        void Capture(string? data)
        {
            if (data is null)
            {
                return;
            }

            lock (output)
            {
                output.AppendLine(data);
            }

            if (echo)
            {
                Log.Line(data);
            }
        }

        process.OutputDataReceived += (_, e) => Capture(e.Data);
        process.ErrorDataReceived += (_, e) => Capture(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        return (process.ExitCode, output.ToString());
    }
}
