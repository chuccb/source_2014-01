namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Managed equivalents of the native KNCSDK STL serializer helpers.
/// Native ordered containers are serialized in traversal order, so the managed
/// write path preserves the same ordering semantics.
/// </summary>
public sealed class NativeStlSerializer(NativePrimitiveSerializer serializer)
{
    private readonly NativePrimitiveSerializer _serializer =
        serializer ?? throw new ArgumentNullException(nameof(serializer));

    public void PutPair<TFirst, TSecond>(
        TFirst first,
        TSecond second,
        Action<NativePrimitiveSerializer, TFirst> putFirst,
        Action<NativePrimitiveSerializer, TSecond> putSecond)
    {
        ArgumentNullException.ThrowIfNull(putFirst);
        ArgumentNullException.ThrowIfNull(putSecond);

        _serializer.WriteCollectionTag(NativePrimitiveSerializer.TagPair);
        putFirst(_serializer, first);
        putSecond(_serializer, second);
    }

    public void PutVector<T>(
        IReadOnlyCollection<T> values,
        Action<NativePrimitiveSerializer, T> put) =>
        PutRange(NativePrimitiveSerializer.TagVector, values, put);

    public void PutList<T>(
        IReadOnlyCollection<T> values,
        Action<NativePrimitiveSerializer, T> put) =>
        PutRange(NativePrimitiveSerializer.TagList, values, put);

    public void PutDeque<T>(
        IReadOnlyCollection<T> values,
        Action<NativePrimitiveSerializer, T> put) =>
        PutRange(NativePrimitiveSerializer.TagDeque, values, put);

    public void PutSet<T>(
        IEnumerable<T> values,
        Action<NativePrimitiveSerializer, T> put,
        IComparer<T>? comparer = null) =>
        PutRange(NativePrimitiveSerializer.TagSet, CreateSortedUniqueValues(values, comparer), put);

    public void PutMultiSet<T>(
        IEnumerable<T> values,
        Action<NativePrimitiveSerializer, T> put,
        IComparer<T>? comparer = null) =>
        PutRange(NativePrimitiveSerializer.TagMultiSet, CreateSortedValues(values, comparer), put);

    public void PutMap<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey, TValue>> values,
        Action<NativePrimitiveSerializer, TKey> putKey,
        Action<NativePrimitiveSerializer, TValue> putValue,
        IComparer<TKey>? comparer = null) =>
        PutMapRange(
            NativePrimitiveSerializer.TagMap,
            CreateSortedMapEntries(values, comparer),
            putKey,
            putValue);

    public void PutMultiMap<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey, TValue>> values,
        Action<NativePrimitiveSerializer, TKey> putKey,
        Action<NativePrimitiveSerializer, TValue> putValue,
        IComparer<TKey>? comparer = null) =>
        PutMapRange(
            NativePrimitiveSerializer.TagMultiMap,
            CreateSortedMapEntries(values, comparer),
            putKey,
            putValue);

    public bool TryGetPair<TFirst, TSecond>(
        out TFirst first,
        out TSecond second,
        Func<NativePrimitiveSerializer, (bool Ok, TFirst Value)> getFirst,
        Func<NativePrimitiveSerializer, (bool Ok, TSecond Value)> getSecond)
    {
        ArgumentNullException.ThrowIfNull(getFirst);
        ArgumentNullException.ThrowIfNull(getSecond);
        first = default!;
        second = default!;

        if (!_serializer.ReadCollectionTag(NativePrimitiveSerializer.TagPair))
            return false;

        var firstResult = getFirst(_serializer);
        if (!firstResult.Ok)
            return false;

        var secondResult = getSecond(_serializer);
        if (!secondResult.Ok)
            return false;

        first = firstResult.Value;
        second = secondResult.Value;
        return true;
    }

    public bool TryGetVector<T>(
        out List<T> values,
        Func<NativePrimitiveSerializer, (bool Ok, T Value)> get) =>
        TryGetRange(NativePrimitiveSerializer.TagVector, out values, get);

    public bool TryGetList<T>(
        out List<T> values,
        Func<NativePrimitiveSerializer, (bool Ok, T Value)> get) =>
        TryGetRange(NativePrimitiveSerializer.TagList, out values, get);

    public bool TryGetDeque<T>(
        out List<T> values,
        Func<NativePrimitiveSerializer, (bool Ok, T Value)> get) =>
        TryGetRange(NativePrimitiveSerializer.TagDeque, out values, get);

    public bool TryGetSet<T>(
        out SortedSet<T> values,
        Func<NativePrimitiveSerializer, (bool Ok, T Value)> get,
        IComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(get);
        var parsedValues = new SortedSet<T>(comparer);

        if (!TryReadCollectionCount(NativePrimitiveSerializer.TagSet, out var count))
        {
            values = [];
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            var result = get(_serializer);
            if (!result.Ok)
            {
                values = [];
                return false;
            }

            parsedValues.Add(result.Value);
        }

        values = parsedValues;
        return true;
    }

    public bool TryGetMultiSet<T>(
        out List<T> values,
        Func<NativePrimitiveSerializer, (bool Ok, T Value)> get) =>
        TryGetRange(NativePrimitiveSerializer.TagMultiSet, out values, get);

    public bool TryGetMap<TKey, TValue>(
        out SortedDictionary<TKey, TValue> values,
        Func<NativePrimitiveSerializer, (bool Ok, TKey Value)> getKey,
        Func<NativePrimitiveSerializer, (bool Ok, TValue Value)> getValue,
        IComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(getKey);
        ArgumentNullException.ThrowIfNull(getValue);
        var parsedValues = new SortedDictionary<TKey, TValue>(comparer);

        if (!TryReadCollectionCount(NativePrimitiveSerializer.TagMap, out var count))
        {
            values = [];
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            var key = getKey(_serializer);
            if (!key.Ok)
            {
                values = [];
                return false;
            }

            var value = getValue(_serializer);
            if (!value.Ok)
            {
                values = [];
                return false;
            }

            // Native std::map::insert() does not replace an existing key.
            parsedValues.TryAdd(key.Value, value.Value);
        }

        values = parsedValues;
        return true;
    }

    public bool TryGetMultiMap<TKey, TValue>(
        out List<KeyValuePair<TKey, TValue>> values,
        Func<NativePrimitiveSerializer, (bool Ok, TKey Value)> getKey,
        Func<NativePrimitiveSerializer, (bool Ok, TValue Value)> getValue)
    {
        ArgumentNullException.ThrowIfNull(getKey);
        ArgumentNullException.ThrowIfNull(getValue);
        var parsedValues = new List<KeyValuePair<TKey, TValue>>();

        if (!TryReadCollectionCount(NativePrimitiveSerializer.TagMultiMap, out var count))
        {
            values = [];
            return false;
        }

        parsedValues.Capacity = count;
        for (var i = 0; i < count; i++)
        {
            var key = getKey(_serializer);
            if (!key.Ok)
            {
                values = [];
                return false;
            }

            var value = getValue(_serializer);
            if (!value.Ok)
            {
                values = [];
                return false;
            }

            parsedValues.Add(new(key.Value, value.Value));
        }

        values = parsedValues;
        return true;
    }

    private void PutRange<T>(
        byte tag,
        IEnumerable<T> values,
        Action<NativePrimitiveSerializer, T> put)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(put);
        _serializer.WriteCollectionTag(tag);

        var materialized = Materialize(values);
        _serializer.Put(checked((uint)materialized.Count));

        foreach (var value in materialized)
            put(_serializer, value);
    }

    private void PutMapRange<TKey, TValue>(
        byte tag,
        IEnumerable<KeyValuePair<TKey, TValue>> values,
        Action<NativePrimitiveSerializer, TKey> putKey,
        Action<NativePrimitiveSerializer, TValue> putValue)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(putKey);
        ArgumentNullException.ThrowIfNull(putValue);
        _serializer.WriteCollectionTag(tag);

        var materialized = Materialize(values);
        _serializer.Put(checked((uint)materialized.Count));

        foreach (var pair in materialized)
        {
            putKey(_serializer, pair.Key);
            putValue(_serializer, pair.Value);
        }
    }

    private bool TryGetRange<T>(
        byte tag,
        out List<T> values,
        Func<NativePrimitiveSerializer, (bool Ok, T Value)> get)
    {
        ArgumentNullException.ThrowIfNull(get);

        if (!TryReadCollectionCount(tag, out var count))
        {
            values = [];
            return false;
        }

        var parsedValues = new List<T>(count);
        for (var i = 0; i < count; i++)
        {
            var result = get(_serializer);
            if (!result.Ok)
            {
                values = [];
                return false;
            }

            parsedValues.Add(result.Value);
        }

        values = parsedValues;
        return true;
    }

    private bool TryReadCollectionCount(byte tag, out int count)
    {
        if (!_serializer.ReadCollectionTag(tag) ||
            !_serializer.TryGet(out uint rawCount) ||
            rawCount > int.MaxValue)
        {
            count = default;
            return false;
        }

        count = (int)rawCount;
        return true;
    }

    private static IReadOnlyList<T> Materialize<T>(IEnumerable<T> values) =>
        values as IReadOnlyList<T> ?? [.. values];

    private static IEnumerable<T> CreateSortedValues<T>(
        IEnumerable<T> values,
        IComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.OrderBy(static value => value, comparer);
    }

    private static IEnumerable<T> CreateSortedUniqueValues<T>(
        IEnumerable<T> values,
        IComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new SortedSet<T>(values, comparer);
    }

    private static IEnumerable<KeyValuePair<TKey, TValue>> CreateSortedMapEntries<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey, TValue>> values,
        IComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.OrderBy(static pair => pair.Key, comparer);
    }
}
