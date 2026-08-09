using System.Buffers.Binary;
using System.Text;

namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Managed implementation of the builtin portion of the native KSerializer.
/// Wire format is verified against KNCSDK/Include/Serializer/Serializer.h/.cpp.
/// </summary>
public sealed class NativePrimitiveSerializer(KSerBuffer buffer, bool tagging = false)
{
    private const byte TagChar = 0;
    private const byte TagWChar = 1;
    private const byte TagUChar = 2;
    private const byte TagShort = 3;
    private const byte TagUShort = 4;
    private const byte TagInt = 5;
    private const byte TagDword = 6;
    private const byte TagInt64 = 7;
    private const byte TagUInt64 = 8;
    private const byte TagFloat = 9;
    private const byte TagDouble = 10;
    private const byte TagBool = 11;
    private const byte TagString = 12;
    private const byte TagWString = 13;
    private const byte TagArray = 14;
    private const byte TagRawBytes = 15;

    private readonly KSerBuffer _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    private readonly bool _tagging = tagging;

    public int ReadLength => _buffer.ReadLength;

    public void Put(sbyte value) => WriteByte(TagChar, unchecked((byte)value));
    public void PutWChar(ushort value) => WriteUInt16(TagWChar, value);
    public void Put(byte value) => WriteByte(TagUChar, value);
    public void Put(short value) => WriteInt16(TagShort, value);
    public void Put(ushort value) => WriteUInt16(TagUShort, value);
    public void Put(int value) => WriteInt32(TagInt, value);
    public void Put(uint value) => WriteUInt32(TagDword, value);
    public void Put(long value) => WriteInt64(TagInt64, value);
    public void Put(ulong value) => WriteUInt64(TagUInt64, value);

    public void Put(float value)
    {
        WriteTag(TagFloat);
        WriteInt32Raw(BitConverter.SingleToInt32Bits(value));
    }

    public void Put(double value)
    {
        WriteTag(TagDouble);
        WriteInt64Raw(BitConverter.DoubleToInt64Bits(value));
    }

    public void Put(bool value) => WriteByte(TagBool, (byte)(value ? 1 : 0));

    /// <summary>
    /// Native std::string format: tag, DWORD byte length, then exactly that many raw bytes.
    /// Native KSerializer does not perform a character encoding conversion.
    /// </summary>
    public void Put(ReadOnlySpan<byte> value)
    {
        WriteLengthPrefixedBytes(TagString, value);
    }

    /// <summary>
    /// Convenience overload for std::string when its application-level encoding is known.
    /// The encoding is not part of the native wire format.
    /// </summary>
    public void Put(string value, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(encoding);
        var bytes = encoding.GetBytes(value);
        Put(bytes);
    }

    /// <summary>
    /// Native const char* overload uses strlen(), therefore an embedded NUL terminates the value.
    /// </summary>
    public void PutCString(string value, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(encoding);

        var nul = value.IndexOf('\0');
        if (nul >= 0)
            value = value[..nul];

        Put(value, encoding);
    }

    /// <summary>
    /// Native std::wstring format on MSVC: tag, DWORD byte length, then raw wchar_t bytes.
    /// MSVC wchar_t is 16-bit and the native serializer does not byte-swap wchar_t data.
    /// </summary>
    public void PutWString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        WriteLengthPrefixedBytes(TagWString, Encoding.Unicode.GetBytes(value));
    }

    /// <summary>Native const wchar_t* overload uses wcslen() semantics.</summary>
    public void PutWCString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var nul = value.IndexOf('\0');
        if (nul >= 0)
            value = value[..nul];

        PutWString(value);
    }

    /// <summary>Raw bytes: tag followed immediately by the supplied bytes; there is no length field.</summary>
    public void PutRaw(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
            throw new ArgumentOutOfRangeException(nameof(value), "Native PutRaw requires len > 0.");

        WriteTag(TagRawBytes);
        _buffer.Write(value);
    }

    public bool TryGet(out sbyte value)
    {
        if (!TryReadByte(TagChar, out var raw))
        {
            value = default;
            return false;
        }

        value = unchecked((sbyte)raw);
        return true;
    }

    public bool TryGetWChar(out ushort value) => TryReadUInt16(TagWChar, out value);
    public bool TryGet(out byte value) => TryReadByte(TagUChar, out value);
    public bool TryGet(out short value) => TryReadInt16(TagShort, out value);
    public bool TryGet(out ushort value) => TryReadUInt16(TagUShort, out value);
    public bool TryGet(out int value) => TryReadInt32(TagInt, out value);
    public bool TryGet(out uint value) => TryReadUInt32(TagDword, out value);
    public bool TryGet(out long value) => TryReadInt64(TagInt64, out value);
    public bool TryGet(out ulong value) => TryReadUInt64(TagUInt64, out value);

    public bool TryGet(out float value)
    {
        if (!TryReadInt32(TagFloat, out var raw))
        {
            value = default;
            return false;
        }

        value = BitConverter.Int32BitsToSingle(raw);
        return true;
    }

    public bool TryGet(out double value)
    {
        if (!TryReadInt64(TagDouble, out var raw))
        {
            value = default;
            return false;
        }

        value = BitConverter.Int64BitsToDouble(raw);
        return true;
    }

    public bool TryGet(out bool value)
    {
        if (!TryReadByte(TagBool, out var raw))
        {
            value = default;
            return false;
        }

        value = raw == 1;
        return true;
    }

    /// <summary>Reads native std::string bytes without assuming an application text encoding.</summary>
    public bool TryGetString(out byte[] value)
    {
        return TryReadLengthPrefixedBytes(TagString, out value);
    }

    public bool TryGetString(out string value, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        if (!TryGetString(out var bytes))
        {
            value = string.Empty;
            return false;
        }

        value = encoding.GetString(bytes);
        return true;
    }

    /// <summary>Reads native MSVC std::wstring (UTF-16 code units stored as raw bytes).</summary>
    public bool TryGetWString(out string value)
    {
        if (!TryReadLengthPrefixedBytes(TagWString, out var bytes) || (bytes.Length & 1) != 0)
        {
            value = string.Empty;
            return false;
        }

        value = Encoding.Unicode.GetString(bytes);
        return true;
    }

    /// <summary>Reads a fixed-size native PutRaw payload. The caller supplies its known length.</summary>
    public bool TryGetRaw(Span<byte> destination)
    {
        if (destination.IsEmpty)
            return false;

        return ReadTag(TagRawBytes) && _buffer.Read(destination);
    }

    /// <summary>
    /// Native PutArray framing: tag, DWORD element count, then each element serialized by Put().
    /// The element operation is explicit so no packed-memory assumption is introduced.
    /// </summary>
    public void PutArray<T>(ReadOnlySpan<T> values, Action<NativePrimitiveSerializer, T> put)
    {
        ArgumentNullException.ThrowIfNull(put);
        WriteTag(TagArray);
        Put((uint)values.Length);

        foreach (var value in values)
            put(this, value);
    }

    /// <summary>Reads the native array framing; callers then decode exactly count serialized elements.</summary>
    public bool TryBeginArray(out uint count)
    {
        if (!ReadTag(TagArray) || !TryGet(out count))
        {
            count = default;
            return false;
        }

        return true;
    }

    private void WriteLengthPrefixedBytes(byte tag, ReadOnlySpan<byte> bytes)
    {
        WriteTag(tag);
        Put((uint)bytes.Length);

        if (!bytes.IsEmpty)
            _buffer.Write(bytes);
    }

    private bool TryReadLengthPrefixedBytes(byte tag, out byte[] bytes)
    {
        bytes = [];

        if (!ReadTag(tag) || !TryGet(out uint size))
            return false;

        if (size > int.MaxValue || size > _buffer.ReadLength)
            return false;

        bytes = GC.AllocateUninitializedArray<byte>((int)size);
        return size == 0 || _buffer.Read(bytes);
    }

    private void WriteByte(byte tag, byte value)
    {
        WriteTag(tag);
        _buffer.Write([value]);
    }

    private void WriteInt16(byte tag, short value)
    {
        WriteTag(tag);
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(b, value);
        _buffer.Write(b);
    }

    private void WriteUInt16(byte tag, ushort value)
    {
        WriteTag(tag);
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(b, value);
        _buffer.Write(b);
    }

    private void WriteInt32(byte tag, int value)
    {
        WriteTag(tag);
        WriteInt32Raw(value);
    }

    private void WriteUInt32(byte tag, uint value)
    {
        WriteTag(tag);
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(b, value);
        _buffer.Write(b);
    }

    private void WriteInt64(byte tag, long value)
    {
        WriteTag(tag);
        WriteInt64Raw(value);
    }

    private void WriteUInt64(byte tag, ulong value)
    {
        WriteTag(tag);
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(b, value);
        _buffer.Write(b);
    }

    private void WriteInt32Raw(int value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(b, value);
        _buffer.Write(b);
    }

    private void WriteInt64Raw(long value)
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(b, value);
        _buffer.Write(b);
    }

    private bool TryReadByte(byte expectedTag, out byte value)
    {
        value = default;
        if (!ReadTag(expectedTag))
            return false;

        Span<byte> b = stackalloc byte[1];
        if (!_buffer.Read(b))
            return false;

        value = b[0];
        return true;
    }

    private bool TryReadInt16(byte expectedTag, out short value)
    {
        value = default;
        Span<byte> b = stackalloc byte[2];
        if (!ReadTag(expectedTag) || !_buffer.Read(b))
            return false;
        value = BinaryPrimitives.ReadInt16BigEndian(b);
        return true;
    }

    private bool TryReadUInt16(byte expectedTag, out ushort value)
    {
        value = default;
        Span<byte> b = stackalloc byte[2];
        if (!ReadTag(expectedTag) || !_buffer.Read(b))
            return false;
        value = BinaryPrimitives.ReadUInt16BigEndian(b);
        return true;
    }

    private bool TryReadInt32(byte expectedTag, out int value)
    {
        value = default;
        Span<byte> b = stackalloc byte[4];
        if (!ReadTag(expectedTag) || !_buffer.Read(b))
            return false;
        value = BinaryPrimitives.ReadInt32BigEndian(b);
        return true;
    }

    private bool TryReadUInt32(byte expectedTag, out uint value)
    {
        value = default;
        Span<byte> b = stackalloc byte[4];
        if (!ReadTag(expectedTag) || !_buffer.Read(b))
            return false;
        value = BinaryPrimitives.ReadUInt32BigEndian(b);
        return true;
    }

    private bool TryReadInt64(byte expectedTag, out long value)
    {
        value = default;
        Span<byte> b = stackalloc byte[8];
        if (!ReadTag(expectedTag) || !_buffer.Read(b))
            return false;
        value = BinaryPrimitives.ReadInt64BigEndian(b);
        return true;
    }

    private bool TryReadUInt64(byte expectedTag, out ulong value)
    {
        value = default;
        Span<byte> b = stackalloc byte[8];
        if (!ReadTag(expectedTag) || !_buffer.Read(b))
            return false;
        value = BinaryPrimitives.ReadUInt64BigEndian(b);
        return true;
    }

    private bool ReadTag(byte expectedTag)
    {
        if (!_tagging)
            return true;

        Span<byte> tag = stackalloc byte[1];
        return _buffer.Read(tag) && tag[0] == expectedTag;
    }

    private void WriteTag(byte tag)
    {
        if (_tagging)
            _buffer.Write([tag]);
    }
}
