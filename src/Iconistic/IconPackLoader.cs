using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Iconistic;

/// <summary>
/// Helpers for loading <see cref="IconStorage.DeployedFile"/> packs at runtime.
/// </summary>
public static class IconPackLoader
{
    /// <summary>Fetches pack JSON from <paramref name="url"/> and loads it into <paramref name="pack"/>.</summary>
    public static async Task LoadAsync(
        this IconPack pack,
        HttpClient client,
        string url,
        CancellationToken cancellation = default)
    {
#if NET8_0_OR_GREATER
        var json = await client.GetStringAsync(url, cancellation);
#else
        var json = await client.GetStringAsync(url);
#endif
        pack.Load(json);
    }
}
