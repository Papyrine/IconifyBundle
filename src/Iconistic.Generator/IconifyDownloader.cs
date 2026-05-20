using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Iconistic.Generator;

/// <summary>
/// Downloads Iconify icon-data JSON, caching responses on disk so the network is only hit on a
/// cache miss. This is the deliberate (RS1035-suppressed) I/O at the core of the generator.
/// </summary>
static class IconifyDownloader
{
    const string Host = "https://api.iconify.design";

    static readonly HttpClient http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static string? GetRawJson(string prefix, IReadOnlyList<string> icons, Settings settings, out string? error)
    {
        var cacheFile = CachePath(settings.CacheDirectory, prefix, icons);

        try
        {
            if (File.Exists(cacheFile))
            {
                error = null;
                return File.ReadAllText(cacheFile);
            }
        }
        catch
        {
            // Fall through to a network fetch if the cache is unreadable.
        }

        if (settings.Offline)
        {
            error = "offline";
            return null;
        }

        try
        {
            var query = string.Join(",", icons.Select(Uri.EscapeDataString));
            var url = $"{Host}/{Uri.EscapeDataString(prefix)}.json?icons={query}";
            var json = http.GetStringAsync(url).GetAwaiter().GetResult();

            try
            {
                var directory = Path.GetDirectoryName(cacheFile);
                if (directory is not null)
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(cacheFile, json);
            }
            catch
            {
                // A failed cache write is non-fatal; we still return the downloaded data.
            }

            error = null;
            return json;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return null;
        }
    }

    static string CachePath(string cacheDirectory, string prefix, IReadOnlyList<string> icons)
    {
        var sorted = icons.OrderBy(_ => _, StringComparer.Ordinal);
        var key = prefix + "\n" + string.Join("\n", sorted);
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));

        var hash = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            hash.Append(b.ToString("x2"));
        }

        return Path.Combine(cacheDirectory, SanitizePrefix(prefix), hash + ".json");
    }

    static string SanitizePrefix(string prefix)
    {
        var builder = new StringBuilder(prefix.Length);
        foreach (var c in prefix)
        {
            builder.Append(char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_');
        }

        return builder.Length == 0 ? "_" : builder.ToString();
    }
}
