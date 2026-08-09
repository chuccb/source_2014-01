namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Serialization helper for native KSerBuffer.
/// Wire format: optional BUFFER tag, DWORD stored-buffer length, then for a
/// non-empty buffer a BOOL compression flag and the stored bytes. On Get the
/// native serializer restores compressed buffers to their uncompressed form.
/// </summary>
public sealed class NativeBufferSerializer(NativePrimitiveSerializer serializer)
{
    private readonly NativePrimitiveSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public void Put(KSerBuffer value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _serializer.WriteCollectionTag(NativePrimitiveSerializer.TagBuffer);
        _serializer.Put(checked((uint)value.Length));

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
        return !compressed || value.UnCompress();
    }
}
