using System.Collections;

namespace PDML.Compiler.Common;

/// <summary>
/// Delimited list where items and delimiters have the same type.
/// </summary>
public sealed class DelimitedList<T> : IEnumerable<T>
{
    private readonly List<T> _entries = new List<T>();
    private bool _expectsItem = true;

    public int Count { get; private set; }

    public int TotalCount => _entries.Count;

    public void AddItem(T item)
    {
        if (!_expectsItem)
            throw new InvalidOperationException(
                "AddDelimiter must be called before adding another item.");

        _entries.Add(item);
        Count++;
        _expectsItem = false;
    }

    public void AddDelimiter(T delimiter)
    {
        if (_expectsItem)
            throw new InvalidOperationException(
                "AddItem must be called before adding a delimiter.");

        _entries.Add(delimiter);
        _expectsItem = true;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _entries.Count; i += 2)
        {
            yield return _entries[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}

/// <summary>
/// Delimited list where items and delimiters have different types.
/// </summary>
public sealed class DelimitedList<TItem, TDelimiter> : IEnumerable<TItem>
{
    private readonly List<object> _entries = new List<object>();
    private bool _expectsItem = true;

    public int Count { get; private set; }

    public int TotalCount => _entries.Count;

    public void AddItem(TItem item)
    {
        if (!_expectsItem)
            throw new InvalidOperationException(
                "AddDelimiter must be called before adding another item.");

        _entries.Add(item!);
        Count++;
        _expectsItem = false;
    }

    public void AddDelimiter(TDelimiter delimiter)
    {
        if (_expectsItem)
            throw new InvalidOperationException(
                "AddItem must be called before adding a delimiter.");

        _entries.Add(delimiter!);
        _expectsItem = true;
    }

    public IEnumerator<TItem> GetEnumerator()
    {
        for (int i = 0; i < _entries.Count; i += 2)
        {
            yield return (TItem)_entries[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}