namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Managed byte buffer matching the native KSerBuffer read/write cursor semantics.
/// Compression remains a separate concern until its native implementation is ported;
/// the serializer itself only relies on the byte-storage contract.
/// </summary>
public sealed class KSerBuffer
{
    private byte[] _buffer;
    private int _writePosition;
    private int _readPosition;

    public KSerBuffer(int initialCapacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);
        _buffer = GC.AllocateUninitializedArray<byte>(initialCapacity);
    }

    public KSerBuffer(ReadOnlySpan<byte> data)
    {
        _buffer = GC.AllocateUninitializedArray<byte>(Math.Max(256, data.Length));
        data.CopyTo(_buffer);
        _writePosition = data.Length;
    }

    public int Length => _writePosition;
    public int ReadLength => _writePosition - _readPosition;
    public int ReadPosition => _readPosition;
    public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _writePosition);

    public void Clear()
    {
        _writePosition = 0;
        _readPosition = 0;
    }

    public void ResetReader() => _readPosition = 0;

    public void Write(ReadOnlySpan<byte> source)
    {
        EnsureCapacity(source.Length);
        source.CopyTo(_buffer.AsSpan(_writePosition));
        _writePosition += source.Length;
    }

    public bool Read(Span<byte> destination)
    {
        if (destination.IsEmpty)
            return false;
        if (destination.Length > ReadLength)
            return false;

        _buffer.AsSpan(_readPosition, destination.Length).CopyTo(destination);
        _readPosition += destination.Length;
        return true;
    }

    public void SetData(ReadOnlySpan<byte> source)
    {
        Clear();
        EnsureCapacity(source.Length);
        Write(source);
    }

    public KSerBuffer Clone() => new(WrittenMemory.Span)
    {
        _readPosition = _readPosition,
    };

    public void Swap(KSerBuffer other)
    {
        ArgumentNullException.ThrowIfNull(other);
        (_buffer, other._buffer) = (other._buffer, _buffer);
        (_writePosition, other._writePosition) = (other._writePosition, _writePosition);
        (_readPosition, other._readPosition) = (other._readPosition, _readPosition);
    }

    private void EnsureCapacity(int additionalBytes)
    {
        var required = checked(_writePosition + additionalBytes);
        if (required <= _buffer.Length)
            return;

        var newSize = Math.Max(required, checked(_buffer.Length * 2));
        Array.Resize(ref _buffer, newSize);
    }
}
