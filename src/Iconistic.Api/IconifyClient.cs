using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Iconistic.Api;

/// <summary>
/// A .NET wrapper over the Iconify API (<see href="https://iconify.design/docs/api"/>).
/// Wraps icon data, server-side SVG and CSS, search and collection metadata.
/// </summary>
public sealed class IconifyClient : IDisposable
{
    /// <summary>The public Iconify API host.</summary>
    public const string DefaultBaseUrl = "https://api.iconify.design";

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    readonly HttpClient client;
    readonly bool ownsClient;
    readonly string baseUrl;

    /// <summary>Creates a client, optionally supplying an <see cref="HttpClient"/> and base URL.</summary>
    public IconifyClient(HttpClient? client = null, string baseUrl = DefaultBaseUrl)
    {
        ownsClient = client is null;
        this.client = client ?? new HttpClient();
        this.baseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>Gets resolved icon data for the named icons in a collection.</summary>
    public async Task<IconDataResult> GetIconDataAsync(
        string prefix,
        IEnumerable<string> icons,
        CancellationToken cancellation = default)
    {
        var list = icons as IReadOnlyList<string> ?? icons.ToArray();
        var query = string.Join(",", list.Select(Uri.EscapeDataString));
        var url = $"{baseUrl}/{Uri.EscapeDataString(prefix)}.json?icons={query}";
        var json = await GetStringAsync(url, cancellation);
        return IconDataParser.Parse(json, list);
    }

    /// <summary>Gets a single resolved icon, or null if it is not found.</summary>
    public async Task<IconisticIcon?> GetIconAsync(
        string prefix,
        string name,
        CancellationToken cancellation = default)
    {
        var data = await GetIconDataAsync(prefix, [name], cancellation);
        return data.Icons.TryGetValue(name, out var icon) ? icon : null;
    }

    /// <summary>Gets a server-rendered SVG string for an icon.</summary>
    public Task<string> GetSvgAsync(
        string prefix,
        string name,
        SvgRequest request = default,
        CancellationToken cancellation = default)
    {
        var url = $"{baseUrl}/{Uri.EscapeDataString(prefix)}/{Uri.EscapeDataString(name)}.svg{request.ToQueryString()}";
        return GetStringAsync(url, cancellation);
    }

    /// <summary>Gets CSS (with embedded icons) for the named icons in a collection.</summary>
    public Task<string> GetCssAsync(
        string prefix,
        IEnumerable<string> icons,
        CancellationToken cancellation = default)
    {
        var query = string.Join(",", icons.Select(Uri.EscapeDataString));
        var url = $"{baseUrl}/{Uri.EscapeDataString(prefix)}.css?icons={query}";
        return GetStringAsync(url, cancellation);
    }

    /// <summary>Searches for icons across all collections.</summary>
    public async Task<SearchResponse> SearchAsync(
        string query,
        int? limit = null,
        int? start = null,
        CancellationToken cancellation = default)
    {
        var url = $"{baseUrl}/search?query={Uri.EscapeDataString(query)}";
        if (limit is { } l)
        {
            url += $"&limit={l}";
        }

        if (start is { } s)
        {
            url += $"&start={s}";
        }

        var json = await GetStringAsync(url, cancellation);
        return JsonSerializer.Deserialize<SearchResponse>(json, JsonOptions) ?? new();
    }

    /// <summary>Lists all available collections, keyed by prefix.</summary>
    public async Task<IReadOnlyDictionary<string, IconCollection>> GetCollectionsAsync(
        CancellationToken cancellation = default)
    {
        var json = await GetStringAsync($"{baseUrl}/collections", cancellation);
        return JsonSerializer.Deserialize<Dictionary<string, IconCollection>>(json, JsonOptions)
               ?? new Dictionary<string, IconCollection>();
    }

    /// <summary>Gets details (categories and icon names) for a single collection.</summary>
    public async Task<CollectionDetail> GetCollectionAsync(
        string prefix,
        CancellationToken cancellation = default)
    {
        var url = $"{baseUrl}/collection?prefix={Uri.EscapeDataString(prefix)}";
        var json = await GetStringAsync(url, cancellation);
        return JsonSerializer.Deserialize<CollectionDetail>(json, JsonOptions) ?? new();
    }

    async Task<string> GetStringAsync(string url, CancellationToken cancellation)
    {
        using var response = await client.GetAsync(url, cancellation);
        response.EnsureSuccessStatusCode();
#if NET8_0_OR_GREATER
        return await response.Content.ReadAsStringAsync(cancellation);
#else
        return await response.Content.ReadAsStringAsync();
#endif
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (ownsClient)
        {
            client.Dispose();
        }
    }
}
