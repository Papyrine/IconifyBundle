namespace IconifyBundle;

/// <summary>
/// Marks a generated pack class (e.g. <c>Feather</c>) with its Iconify <see cref="Prefix"/>. The
/// IconifyBundle source generator scans the consuming compilation for static member accesses on types
/// bearing this attribute to discover which icons are actually used, then materialises only those
/// (inline data in Resource mode, on-disk files in Disk mode).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IconifyPackAttribute(string prefix) :
    Attribute
{
    /// <summary>The Iconify prefix for the pack, e.g. <c>feather</c>.</summary>
    public string Prefix { get; } = prefix;
}
