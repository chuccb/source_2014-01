namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Managed equivalents of the native KNCSDK STL serializer helpers.
/// The native format is: container tag (when tagging is enabled), DWORD count,
/// then each element serialized through KSerializer::Put/Get.
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

    public void PutSet<T>(IReadOnlyCollection<T> values, Action<NativePrimitiveSerializer, T> put)
        => PutRange(NativePrimitiveSerializer.TagSet, values, put);

    public void PutMultiSet<T>(IReadOnlyCollection<T> values, Action<NativePrimitiveSerializer, T> put)
        => PutRange(NativePrimitiveSerializer.TagMultiSet, values, put);

    public void PutMap<TKey, TValue>(IReadOnlyCollection<KeyValuePair<TKey, TValue>> values,
        Action<NativePrimitiveSerializer, TKey> putKey,
        Action<NativePrimitiveSerializer, TValue> putValue)
        => PutMapRange(NativePrimitiveSerializer.TagMap, values, putKey, putValue);

    public void PutMultiMap<TKey, TValue>(IReadOnlyCollection<KeyValuePair<TKey, TValue>> values,
        Action<NativePrimitiveSerializer, TKey> putKey,
        Action<NativePrimitiveSerializer, TValue> putValue)
        => PutMapRange(NativePrimitiveSerializer.TagMultiMap, values, putKey, putValue);

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

    public bool TryGetSet<T>(out HashSet<T> values, Func<NativePrimitiveSerializer, (bool Ok, T Value)> get)
    {
        ArgumentNullException.ThrowIfNull(get);
        values = [];
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

    public bool TryGetMap<TKey, TValue>(out Dictionary<TKey, TValue> values,
        Func<NativePrimitiveSerializer, (bool Ok, TKey Value)> getKey,
        Func<NativePrimitiveSerializer, (bool Ok, TValue Value)> getValue)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(getKey);
        ArgumentNullException.ThrowIfNull(getValue);
        values = [];

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

    private void PutRange<T>(byte tag, IReadOnlyCollection<T> values, Action<NativePrimitiveSerializer, T> put)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(put);
        _serializer.WriteCollectionTag(tag);
        _serializer.Put((uint)values.Count);

        foreach (var value in values)
            put(_serializer, value);
    }

    private void PutMapRange<TKey, TValue>(byte tag, IReadOnlyCollection<KeyValuePair<TKey, TValue>> values,
        Action<NativePrimitiveSerializer, TKey> putKey,
        Action<NativePrimitiveSerializer, TValue> putValue)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(putKey);
        ArgumentNullException.ThrowIfNull(putValue);
        _serializer.WriteCollectionTag(tag);
        _serializer.Put((uint)values.Count);

        foreach (var pair in values)
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
}
