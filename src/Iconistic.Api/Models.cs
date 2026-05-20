namespace Iconistic.Api;

/// <summary>Result of <see cref="IconifyClient.GetIconDataAsync"/>: resolved icons plus any misses.</summary>
public sealed record IconDataResult
{
    /// <summary>The resolved icons, keyed by the requested name.</summary>
    public IReadOnlyDictionary<string, IconisticIcon> Icons { get; init; } =
        new Dictionary<string, IconisticIcon>();

    /// <summary>Requested names that were not present in the collection.</summary>
    public IReadOnlyList<string> NotFound { get; init; } = [];
}

/// <summary>An entry from the Iconify <c>/search</c> endpoint.</summary>
public sealed record SearchResponse
{
    /// <summary>Matching icon names in <c>prefix:name</c> form.</summary>
    public IReadOnlyList<string> Icons { get; init; } = [];

    /// <summary>Total number of matches available.</summary>
    public int Total { get; init; }

    /// <summary>The limit that was applied.</summary>
    public int Limit { get; init; }

    /// <summary>The start offset that was applied.</summary>
    public int Start { get; init; }
}

/// <summary>An entry from the Iconify <c>/collections</c> endpoint.</summary>
public sealed record IconCollection
{
    /// <summary>Display name of the collection.</summary>
    public string Name { get; init; } = "";

    /// <summary>Number of icons in the collection.</summary>
    public int Total { get; init; }

    /// <summary>The collection category, e.g. "Material".</summary>
    public string? Category { get; init; }

    /// <summary>The author of the collection.</summary>
    public IconAuthor? Author { get; init; }

    /// <summary>The license of the collection.</summary>
    public IconLicense? License { get; init; }

    /// <summary>Sample icon names.</summary>
    public IReadOnlyList<string>? Samples { get; init; }

    /// <summary>Default icon height.</summary>
    public int? Height { get; init; }

    /// <summary>Whether the icons have a fixed palette (multi-color).</summary>
    public bool? Palette { get; init; }
}

/// <summary>Collection author metadata.</summary>
public sealed record IconAuthor
{
    public string? Name { get; init; }
    public string? Url { get; init; }
}

/// <summary>Collection license metadata.</summary>
public sealed record IconLicense
{
    public string? Title { get; init; }
    public string? Spdx { get; init; }
    public string? Url { get; init; }
}

/// <summary>Details of one collection from the Iconify <c>/collection?prefix=</c> endpoint.</summary>
public sealed record CollectionDetail
{
    /// <summary>The collection prefix.</summary>
    public string Prefix { get; init; } = "";

    /// <summary>Display title of the collection.</summary>
    public string? Title { get; init; }

    /// <summary>Number of icons in the collection.</summary>
    public int Total { get; init; }

    /// <summary>Icons that are not assigned to a category.</summary>
    public IReadOnlyList<string>? Uncategorized { get; init; }

    /// <summary>Icon names grouped by category.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Categories { get; init; }
}
