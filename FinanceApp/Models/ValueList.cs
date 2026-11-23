using System.Collections;

namespace FinanceApp.Models;

/// <summary>
/// A read-only list equal to another with the same items in the same order, so records holding one (e.g.
/// <see cref="MonthlyEntryProjection"/>) stay equal when rebuilt from unchanged data.
/// </summary>
public sealed class ValueList<T> : IReadOnlyList<T>, IEquatable<ValueList<T>>
{
    public static readonly ValueList<T> Empty = new([]);

    private readonly T[] _items;

    public ValueList(IEnumerable<T> items)
    {
        _items = [.. items];
    }

    public int Count => _items.Length;

    public T this[int index] => _items[index];

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public bool Equals(ValueList<T>? other) => other is not null && _items.SequenceEqual(other._items);

    public override bool Equals(object? obj) => obj is ValueList<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in _items)
            hash.Add(item);
        return hash.ToHashCode();
    }
}
