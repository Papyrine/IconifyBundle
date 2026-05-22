static class Log
{
    // Writes straight to the real stdout, bypassing TUnit's per-test Console capture (which only flushes
    // when the test finishes). Lets the long-running PackBuilder stream progress live to the CI log.
    static readonly TextWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

    public static void Line(string message)
    {
        lock (writer)
        {
            writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
