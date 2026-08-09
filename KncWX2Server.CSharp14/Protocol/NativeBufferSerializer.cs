namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Serialization helper for the native KSerBuffer builtin class.
/// Native format: optional BUFFER tag, DWORD stored-buffer length, and, when non-empty,
/// BOOL compression flag followed by the stored bytes. The bytes are not implicitly uncompressed.
/// </summary>
public sealed class NativeBufferSerializer(NativePrimitiveSerializer serializer)
{
    private readonly NativePrimitiveSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public void Put(KSerBuffer value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _serializer.WriteCollectionTag(NativePrimitiveSerializer.TagBuffer);
        _serializer.Put((uint)value.Length);

        if (value.Length == 0)
            return;

        _serializer.Put(value.IsCompressed);
        _serializer.PutRaw(value.WrittenMemory.Span);
    }

    public bool TryGet(KSerBuffer value)
    {
        ArgumentNullException.ThrowIfNull(value);
        value.Clear();

        if (!_serializer.ReadCollectionTag(NativePrimitiveSerializer.TagBuffer) ||
            !_serializer.TryGet(out uint length))
            return false;

        if (length == 0)
            return true;
        if (length > int.MaxValue || length > _serializer.ReadLength)
            return false;
        if (!_serializer.TryGet(out bool compressed))
            return false;

        var bytes = GC.AllocateUninitializedArray<byte>((int)length);
        if (!_serializer.TryGetRaw(bytes))
            return false;

        value.SetData(bytes, compressed);
        return true;
    }
}
