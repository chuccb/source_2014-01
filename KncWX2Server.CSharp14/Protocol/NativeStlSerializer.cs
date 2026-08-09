namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Managed equivalents of the native KNCSDK STL serializer helpers.
/// Native std::set/std::multiset/std::map/std::multimap serialize in their
/// ordered traversal, so the managed write path preserves that ordering.
/// </summary>
public sealed class NativeStlSerializer(NativePrimitiveSerializer serializer)
{
    private readonly NativePrimitiveSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public void PutPair<T1, T2>(T1 first, T2 second, Action<NativePrimitiveSerializer, T1> putFirst, Action<NativePrimitiveSerializer, T2> putSecond)
    {
        ArgumentNullException.ThrowIfNull(putFirst);
        ArgumentNullException.ThrowIfNull(putSecond);
        _serializer.WriteCollectionTag(NativePrimitiveSerializer.TagPair);
        putFirst(_serializer, first);
        putSecond(_serializer, second);
    }

    public void PutVector<T>(IReadOnlyCollection<T> values, Action<NativePrimitiveSerializer, T> put)
        => PutRange(NativePrimitiveSerializer.TagVector, values, put);

    public void PutList<T>(IReadOnlyCollection<T> values, Action<NativePrimitiveSerializer, T> put)
        => PutRange(NativePrimitiveSerializer.TagList, values, put);

    public void PutDeque<T>(IReadOnlyCollection<T> values, Action<NativePrimitiveSerializer, T> put)
        => PutRange(NativePrimitiveSerializer.TagDeque, values, put);

    public void PutSet<T>(IEnumerable<T> values, Action<NativePrimitiveSerializer, T> put, IComparer<T>? comparer = null)
        => PutRange(NativePrimitiveSerializer.TagSet, SortUnique(values, comparer), put);

    public void PutMultiSet<T>(IEnumerable<T> values, Action<NativePrimitiveSerializer, T> put, IComparer<T>? comparer = null)
        => PutRange(NativePrimitiveSerializer.TagMultiSet, Sort(values, comparer), put);

    public void PutMap<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> values,
        Action<NativePrimitiveSerializer, TKey> putKey,
        Action<NativePrimitiveSerializer, TValue> putValue,
        IComparer<TKey>? comparer = null)
        => PutMapRange(NativePrimitiveSerializer.TagMap, SortMap(values, comparer), putKey, putValue);

    public void PutMultiMap<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> values,
        Action<NativePrimitiveSerializer, TKey> putKey,
        Action<NativePrimitiveSerializer, TValue> putValue,
        IComparer<TKey>? comparer = null)
        => PutMapRange(NativePrimitiveSerializer.TagMultiMap, SortMap(values, comparer), putKey, putValue);

    public bool TryGetPair<T1, T2>(out T1 first, out T2 second,
        Func<NativePrimitiveSerializer, (bool Ok, T1 Value)> getFirst,
        Func<NativePrimitiveSerializer, (bool Ok, T2 Value)> getSecond)
    {
        ArgumentNullException.ThrowIfNull(getFirst);
        ArgumentNullException.ThrowIfNull(getSecond);
        first = default!;
        second = default!;

        if (!_serializer.ReadCollectionTag(NativePrimitiveSerializer.TagPair))
            return false;

        var a = getFirst(_serializer);
        if (!a.Ok)
            return false;
        var b = getSecond(_serializer);
        if (!b.Ok)
            return false;

        first = a.Value;
        second = b.Value;
        return true;
    }

    public bool TryGetVector<T>(out List<T> values, Func<NativePrimitiveSerializer, (bool Ok, T Value)> get)
        => TryGetRange(NativePrimitiveSerializer.TagVector, out values, get);

    public bool TryGetList<T>(out List<T> values, Func<NativePrimitiveSerializer, (bool Ok, T Value)> get)
        => TryGetRange(NativePrimitiveSerializer.TagList, out values, get);

    public bool TryGetDeque<T>(out List<T> values, Func<NativePrimitiveSerializer, (bool Ok, T Value)> get)
        => TryGetRange(NativePrimitiveSerializer.TagDeque, out values, get);

    public bool TryGetSet<T>(out SortedSet<T> values, Func<NativePrimitiveSerializer, (bool Ok, T Value)> get, IComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(get);
        values = new SortedSet<T>(comparer);
        if (!TryReadCount(NativePrimitiveSerializer.TagSet, out var count))
            return false;

        for (var i = 0; i < count; i++)
        {
            var result = get(_serializer);
            if (!result.Ok)
                return false;
            values.Add(result.Value);
        }

        return true;
    }

    public bool TryGetMultiSet<T>(out List<T> values, Func<NativePrimitiveSerializer, (bool Ok, T Value)> get)
        => TryGetRange(NativePrimitiveSerializer.TagMultiSet, out values, get);

    public bool TryGetMap<TKey, TValue>(out SortedDictionary<TKey, TValue> values,
        Func<NativePrimitiveSerializer, (bool Ok, TKey Value)> getKey,
        Func<NativePrimitiveSerializer, (bool Ok, TValue Value)> getValue,
        IComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(getKey);
        ArgumentNullException.ThrowIfNull(getValue);
        values = new SortedDictionary<TKey, TValue>(comparer);

        if (!TryReadCount(NativePrimitiveSerializer.TagMap, out var count))
            return false;

        for (var i = 0; i < count; i++)
        {
            var key = getKey(_serializer);
            if (!key.Ok)
                return false;
            var value = getValue(_serializer);
            if (!value.Ok)
                return false;

            // Native std::map::insert() does not replace an existing key.
            values.TryAdd(key.Value, value.Value);
        }

        return true;
    }

    public bool TryGetMultiMap<TKey, TValue>(out List<KeyValuePair<TKey, TValue>> values,
        Func<NativePrimitiveSerializer, (bool Ok, TKey Value)> getKey,
        Func<NativePrimitiveSerializer, (bool Ok, TValue Value)> getValue)
    {
        ArgumentNullException.ThrowIfNull(getKey);
        ArgumentNullException.ThrowIfNull(getValue);
        values = [];

        if (!TryReadCount(NativePrimitiveSerializer.TagMultiMap, out var count))
            return false;

        for (var i = 0; i < count; i++)
        {
            var key = getKey(_serializer);
            if (!key.Ok)
                return false;
            var value = getValue(_serializer);
            if (!value.Ok)
                return false;
            values.Add(new(key.Value, value.Value));
        }

        return true;
    }

    private void PutRange<T>(byte tag, IEnumerable<T> values, Action<NativePrimitiveSerializer, T> put)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(put);
        _serializer.WriteCollectionTag(tag);

        var materialized = values as ICollection<T> ?? values.ToArray();
        _serializer.Put(checked((uint)materialized.Count));

        foreach (var value in materialized)
            put(_serializer, value);
    }

    private void PutMapRange<TKey, TValue>(byte tag, IEnumerable<KeyValuePair<TKey, TValue>> values,
        Action<NativePrimitiveSerializer, TKey> putKey,
        Action<NativePrimitiveSerializer, TValue> putValue)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(putKey);
        ArgumentNullException.ThrowIfNull(putValue);
        _serializer.WriteCollectionTag(tag);

        var materialized = values as ICollection<KeyValuePair<TKey, TValue>> ?? values.ToArray();
        _serializer.Put(checked((uint)materialized.Count));

        foreach (var pair in materialized)
        {
            putKey(_serializer, pair.Key);
            putValue(_serializer, pair.Value);
        }
    }

    private bool TryGetRange<T>(byte tag, out List<T> values, Func<NativePrimitiveSerializer, (bool Ok, T Value)> get)
    {
        ArgumentNullException.ThrowIfNull(get);
        values = [];
        if (!TryReadCount(tag, out var count))
            return false;
        if (count > int.MaxValue)
            return false;

        values = new List<T>((int)count);
        for (var i = 0; i < count; i++)
        {
            var result = get(_serializer);
            if (!result.Ok)
                return false;
            values.Add(result.Value);
        }

        return true;
    }

    private bool TryReadCount(byte tag, out uint count)
    {
        if (!_serializer.ReadCollectionTag(tag) || !_serializer.TryGet(out count))
        {
            count = default;
            return false;
        }

        return true;
    }

    private static IEnumerable<T> Sort<T>(IEnumerable<T> values, IComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.OrderBy(static value => value, comparer);
    }

    private static IEnumerable<T> SortUnique<T>(IEnumerable<T> values, IComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(values);
        var set = new SortedSet<T>(values, comparer);
        return set;
    }

    private static IEnumerable<KeyValuePair<TKey, TValue>> SortMap<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey, TValue>> values,
        IComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.OrderBy(static pair => pair.Key, comparer);
    }
}
