namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Serialization helper for the native KSerBuffer builtin class.
/// Native format: optional BUFFER tag, DWORD payload length, BOOL compression flag,
/// then the payload as raw bytes. An empty buffer does not carry the compression flag.
/// </summary>
public sealed class NativeBufferSerializer(NativePrimitiveSerializer serializer)
{
    private const byte TagBuffer = 24;
    private readonly NativePrimitiveSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public void Put(KSerBuffer value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _serializer.WriteCollectionTag(TagBuffer);
        _serializer.Put((uint)value.Length);

        if (value.Length == 0)
            return;

        _serializer.Put(false);
        _serializer.PutRaw(value.WrittenMemory.Span);
    }

    public bool TryGet(KSerBuffer value)
    {
        ArgumentNullException.ThrowIfNull(value);
        value.Clear();

        if (!_serializer.ReadCollectionTag(TagBuffer) || !_serializer.TryGet(out uint length))
            return false;

        if (length == 0)
            return true;
        if (length > int.MaxValue || length > _serializer.ReadLength)
            return false;
        if (!_serializer.TryGet(out bool compressed) || compressed)
            return false;

        var bytes = GC.AllocateUninitializedArray<byte>((int)length);
        if (!_serializer.TryGetRaw(bytes))
            return false;

        value.Write(bytes);
        return true;
    }
}
