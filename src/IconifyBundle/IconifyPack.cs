namespace IconifyBundle;

/// <summary>
/// A pack parsed from iconify-format JSON: its <see cref="Prefix"/>, the contained
/// <see cref="Icons"/>, and any optional <see cref="Info"/> metadata. Returned by
/// <see cref="IconifyJson.Read(Stream)"/> and the other read APIs.
/// </summary>
public sealed record IconifyPack(
    string Prefix,
    IReadOnlyList<Icon> Icons,
    IconifyPackInfo? Info = null);
