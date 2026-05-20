using System.Collections;

namespace Iconistic.Generator;

/// <summary>
/// An immutable array with value (sequence) equality, so it can be used as an incremental
/// generator pipeline value without defeating Roslyn's output caching.
/// </summary>
readonly struct EquatableArray<T>(T[] items) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    readonly T[] items = items;

    public int Count => items.Length;

    public T this[int index] => items[index];

    public bool Equals(EquatableArray<T> other)
    {
        if (items.Length != other.items.Length)
        {
            return false;
        }

        for (var i = 0; i < items.Length; i++)
        {
            if (!items[i].Equals(other.items[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) =>
        obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in items)
        {
            hash = hash * 31 + (item?.GetHashCode() ?? 0);
        }

        return hash;
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>) items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

    public static EquatableArray<T> From(IEnumerable<T> source) => new([.. source]);
}
