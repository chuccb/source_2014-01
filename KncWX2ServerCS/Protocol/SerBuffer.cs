using System.Collections.ObjectModel;
using System.IO.Compression;

namespace KncWX2Server.Protocol;

/// <summary>Managed port of native KSerBuffer byte storage/read-cursor semantics.</summary>
public sealed class KSerBuffer : IEquatable<KSerBuffer>
{
    private List<byte> _buffer = [];
    private int _readOffset;
    private bool _compressed;

    public int Length => _buffer.Count;
    public int ReadLength => _buffer.Count - _readOffset;
    public bool IsCompressed => _compressed;
    public ReadOnlyMemory<byte> Data => [.. _buffer];
    public ReadOnlyCollection<byte> Buffer => _buffer.AsReadOnly();

    public bool Write(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) throw new ArgumentException("Native KSerBuffer::Write requires len > 0.", nameof(data));
        foreach (byte value in data) _buffer.Add(value);
        return true;
    }

    public bool Read(Span<byte> destination)
    {
        if (destination.IsEmpty) throw new ArgumentException("Native KSerBuffer::Read requires len > 0.", nameof(destination));
        if (destination.Length > ReadLength) return false;
        _buffer.AsSpan(_readOffset, destination.Length).CopyTo(destination);
        _readOffset += destination.Length;
        return true;
    }

    public void Clear()
    {
        _buffer.Clear();
        _readOffset = 0;
        _compressed = false;
    }

    public void Reset() => _readOffset = 0;

    internal void MarkCompressed() => _compressed = true;

    public KSerBuffer Clone() => new(this);

    public void Swap(KSerBuffer other)
    {
        ArgumentNullException.ThrowIfNull(other);
        (_buffer, other._buffer) = (other._buffer, _buffer);
        (_readOffset, other._readOffset) = (other._readOffset, _readOffset);
        (_compressed, other._compressed) = (other._compressed, _compressed);
    }

    public bool Compress()
    {
        if (_compressed) return true;
        byte[] source = [.. _buffer];
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Fastest, leaveOpen: true))
            zlib.Write(source);
        byte[] compressed = output.ToArray();
        Clear();
        Write(BitConverter.GetBytes(source.Length));
        Write(compressed);
        _compressed = true;
        return true;
    }

    public bool UnCompress()
    {
        if (!_compressed) return true;
        Reset();
        Span<byte> sizeBytes = stackalloc byte[4];
        if (!Read(sizeBytes)) return false;
        int originalSize = BitConverter.ToInt32(sizeBytes);
        if (originalSize < 0 || originalSize > 256 * 1024 * 1024) return false;
        byte[] compressed = new byte[ReadLength];
        if (!Read(compressed)) return false;
        try
        {
            using var input = new MemoryStream(compressed, writable: false);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream(originalSize);
            zlib.CopyTo(output);
            byte[] decompressed = output.ToArray();
            if (decompressed.Length != originalSize) return false;
            Clear();
            Write(decompressed);
            return true;
        }
        catch (InvalidDataException) { return false; }
    }

    public bool Equals(KSerBuffer? other) => other is not null && _buffer.SequenceEqual(other._buffer);
    public override bool Equals(object? obj) => obj is KSerBuffer other && Equals(other);
    public override int GetHashCode() => _buffer.Aggregate(17, (hash, value) => unchecked(hash * 31 + value));

    private KSerBuffer(KSerBuffer other)
    {
        _buffer = [.. other._buffer];
        _readOffset = other._readOffset;
        _compressed = other._compressed;
    }

    public KSerBuffer() { }
}
