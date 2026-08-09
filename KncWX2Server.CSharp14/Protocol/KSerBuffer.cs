using System.Buffers.Binary;
using System.IO.Compression;

namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Managed counterpart of the native KSerBuffer.
/// The stored bytes, read cursor, compression flag, and compressed representation
/// follow SerBuffer.h/SerBuffer.cpp rather than merely modeling a generic byte buffer.
/// </summary>
public sealed class KSerBuffer
{
    private byte[] _buffer;
    private int _writePosition;
    private int _readPosition;
    private bool _compressed;

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
    public bool IsCompressed => _compressed;
    public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _writePosition);
    public ReadOnlyMemory<byte> ReadableMemory => _buffer.AsMemory(_readPosition, ReadLength);

    public void Clear()
    {
        _writePosition = 0;
        _readPosition = 0;
        _compressed = false;
    }

    public void Reset() => _readPosition = 0;
    public void ResetReader() => _readPosition = 0;

    public void Write(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
            throw new ArgumentOutOfRangeException(nameof(source), "Native KSerBuffer::Write requires len > 0.");

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

    public void SetData(ReadOnlySpan<byte> source, bool compressed = false)
    {
        Clear();
        if (!source.IsEmpty)
            Write(source);
        _compressed = compressed;
    }

    public KSerBuffer Clone()
    {
        var clone = new KSerBuffer(WrittenMemory.Span)
        {
            _readPosition = _readPosition,
            _compressed = _compressed,
        };
        return clone;
    }

    public void Swap(KSerBuffer other)
    {
        ArgumentNullException.ThrowIfNull(other);
        (_buffer, other._buffer) = (other._buffer, _buffer);
        (_writePosition, other._writePosition) = (other._writePosition, _writePosition);
        (_readPosition, other._readPosition) = (other._readPosition, _readPosition);
        (_compressed, other._compressed) = (other._compressed, _compressed);
    }

    /// <summary>
    /// Compresses the current stored bytes using zlib level 1, matching native
    /// compress2(..., 1). The resulting representation is
    /// [DWORD originalLength in native little-endian][zlib bytes].
    /// Native also compresses an empty buffer; it does not special-case length 0.
    /// </summary>
    public bool Compress()
    {
        if (_compressed)
            return true;

        var source = WrittenMemory;
        using var output = new MemoryStream();
        Span<byte> originalLength = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(originalLength, checked((uint)source.Length));
        output.Write(originalLength);

        using (var zlib = new ZLibStream(output, CompressionLevel.Fastest, leaveOpen: true))
            zlib.Write(source.Span);

        var compressed = output.ToArray();
        Clear();
        Write(compressed);
        _compressed = true;
        return true;
    }

    /// <summary>
    /// Restores a buffer produced by Compress(). The DWORD stored before the zlib
    /// stream is native little-endian because native Compress() writes it directly
    /// with KSerBuffer::Write().
    /// </summary>
    public bool UnCompress()
    {
        if (!_compressed)
            return true;
        if (_writePosition < sizeof(uint))
            return false;

        var originalLength = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(0, sizeof(uint)));
        if (originalLength > int.MaxValue)
            return false;

        try
        {
            using var input = new MemoryStream(_buffer, sizeof(uint), _writePosition - sizeof(uint), writable: false);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            var restored = GC.AllocateUninitializedArray<byte>((int)originalLength);
            var offset = 0;

            while (offset < restored.Length)
            {
                var read = zlib.Read(restored, offset, restored.Length - offset);
                if (read == 0)
                    return false;
                offset += read;
            }

            // Native uncompress() is given the complete remaining input length and
            // validates the decompressed byte count, but does not separately reject
            // trailing bytes. Do not impose a stricter framing rule here.
            Clear();
            if (restored.Length != 0)
                Write(restored);
            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    public bool Equals(KSerBuffer? other)
    {
        if (other is null || Length != other.Length)
            return false;
        return WrittenMemory.Span.SequenceEqual(other.WrittenMemory.Span);
    }

    public override bool Equals(object? obj) => obj is KSerBuffer other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Length, WrittenMemory.Span.Length == 0 ? 0 : WrittenMemory.Span[0]);

    private void EnsureCapacity(int additionalBytes)
    {
        var required = checked(_writePosition + additionalBytes);
        if (required <= _buffer.Length)
            return;

        var newSize = Math.Max(required, checked(_buffer.Length * 2));
        Array.Resize(ref _buffer, newSize);
    }
}
