namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Managed counterpart of native KKeyedSerializer&lt;KEY&gt;.
/// Each stored object is independently serialized into a KSerBuffer using the
/// keyed serializer's tagging mode. The serialized keyed object is a BOOL
/// tagging-state followed by a native map of KEY to KSerBuffer.
/// </summary>
public sealed class NativeKeyedSerializer<TKey> where TKey : notnull
{
    private const byte TagBuffer = 24;

    private readonly Dictionary<TKey, KSerBuffer> _objects = [];
    private bool _useTagging;

    public bool UseTagging => _useTagging;
    public int Count => _objects.Count;

    public void SetTagging(bool useTagging) => _useTagging = useTagging;

    public bool HasKey(TKey key) => _objects.ContainsKey(key);

    public IReadOnlyList<TKey> GetKeys() => [.. _objects.Keys];

    public bool Erase(TKey key) => _objects.Remove(key);

    public void Clear() => _objects.Clear();

    public void Put<T>(TKey key, T value, Action<NativePrimitiveSerializer, T> put)
    {
        ArgumentNullException.ThrowIfNull(put);

        var buffer = new KSerBuffer();
        var serializer = new NativePrimitiveSerializer(buffer, _useTagging);
        put(serializer, value);
        _objects[key] = buffer;
    }

    public bool TryGet<T>(
        TKey key,
        out T value,
        Func<NativePrimitiveSerializer, (bool Ok, T Value)> get)
    {
        ArgumentNullException.ThrowIfNull(get);
        value = default!;

        if (!_objects.TryGetValue(key, out var stored))
            return false;

        var buffer = stored.Clone();
        var serializer = new NativePrimitiveSerializer(buffer, _useTagging);
        var result = get(serializer);
        if (!result.Ok)
            return false;

        value = result.Value;
        return true;
    }

    public void PutInto(
        NativePrimitiveSerializer serializer,
        Action<NativePrimitiveSerializer, TKey> putKey)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(putKey);

        // Native KKeyedSerializer::PutInto(): ser.Put(m_useTagging), then ser.Put(m_objects).
        serializer.Put(_useTagging);

        var entries = _objects
            .Select(static pair => new KeyValuePair<TKey, KSerBuffer>(pair.Key, pair.Value))
            .ToArray();

        var stl = new NativeStlSerializer(serializer);
        stl.PutMap(entries, putKey, static (ser, buffer) => PutBuffer(ser, buffer));
    }

    public bool TryGetFrom(
        NativePrimitiveSerializer serializer,
        Func<NativePrimitiveSerializer, (bool Ok, TKey Value)> getKey)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(getKey);

        if (!serializer.TryGet(out bool tagging))
            return false;

        var stl = new NativeStlSerializer(serializer);
        if (!stl.TryGetMap(out Dictionary<TKey, KSerBuffer> values, getKey, TryGetBuffer))
            return false;

        _objects.Clear();
        foreach (var pair in values)
            _objects.Add(pair.Key, pair.Value);

        _useTagging = tagging;
        return true;
    }

    public bool Equals(NativeKeyedSerializer<TKey>? other)
    {
        if (other is null || _useTagging != other._useTagging || _objects.Count != other._objects.Count)
            return false;

        foreach (var pair in _objects)
        {
            if (!other._objects.TryGetValue(pair.Key, out var otherBuffer))
                return false;
            if (!pair.Value.WrittenMemory.Span.SequenceEqual(otherBuffer.WrittenMemory.Span))
                return false;
        }

        return true;
    }

    private static void PutBuffer(NativePrimitiveSerializer serializer, KSerBuffer buffer)
    {
        serializer.WriteCollectionTag(TagBuffer);
        serializer.Put((uint)buffer.Length);

        if (buffer.Length == 0)
            return;

        // KKeyedSerializer creates its stored buffers through BeginWriting and
        // does not compress them, so the serialized buffer's flag is false here.
        serializer.Put(false);
        serializer.PutRaw(buffer.WrittenMemory.Span);
    }

    private static (bool Ok, KSerBuffer Value) TryGetBuffer(NativePrimitiveSerializer serializer)
    {
        var buffer = new KSerBuffer();
        if (!serializer.ReadCollectionTag(TagBuffer) || !serializer.TryGet(out uint length))
            return (false, buffer);
        if (length == 0)
            return (true, buffer);
        if (length > int.MaxValue || length > serializer.ReadLength)
            return (false, buffer);
        if (!serializer.TryGet(out bool compressed) || compressed)
            return (false, buffer);

        var bytes = GC.AllocateUninitializedArray<byte>((int)length);
        if (!serializer.TryGetRaw(bytes))
            return (false, buffer);

        buffer.SetData(bytes);
        return (true, buffer);
    }
}
