using System.Buffers.Binary;
using System.Text;

namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Managed implementation of the builtin portion of the native KSerializer.
/// </summary>
public sealed class NativePrimitiveSerializer(KSerBuffer buffer, bool tagging = false)
{
    internal const byte TagPair = 16;
    internal const byte TagVector = 17;
    internal const byte TagList = 18;
    internal const byte TagDeque = 19;
    internal const byte TagSet = 20;
    internal const byte TagMultiSet = 21;
    internal const byte TagMap = 22;
    internal const byte TagMultiMap = 23;
    internal const byte TagBuffer = 24;
    internal const byte TagKeyedSerializer = 25;
    internal const byte TagUserClass = 26;

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

    public void Put(bool value) => WriteByte(TagBool, value ? (byte)1 : (byte)0);

    /// <summary>
    /// Native std::string is serialized as a length-prefixed byte sequence.
    /// The existing C# protocol layer uses UTF-8 as its default application encoding.
    /// </summary>
    public void Put(string value) => Put(value, Encoding.UTF8);

    public void Put(ReadOnlySpan<byte> value) => WriteLengthPrefixedBytes(TagString, value);

    public void Put(string value, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(encoding);

        Put(encoding.GetBytes(value));
    }

    public void PutCString(string value, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(encoding);

        var nulIndex = value.IndexOf('\0');
        Put(value[..(nulIndex < 0 ? value.Length : nulIndex)], encoding);
    }

    /// <summary>
    /// Native MSVC wchar_t is 16-bit. The original serializer writes the UTF-16
    /// code units directly, so this intentionally uses little-endian byte order.
    /// </summary>
    public void PutWString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        WriteLengthPrefixedBytes(TagWString, Encoding.Unicode.GetBytes(value));
    }

    public void PutWide(string value) => PutWString(value);

    public void PutWCString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var nulIndex = value.IndexOf('\0');
        PutWString(value[..(nulIndex < 0 ? value.Length : nulIndex)]);
    }

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

    public bool TryGetWChar(out ushort value) => TryReadUInt16LittleEndian(TagWChar, out value);
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

    public bool TryGetString(out string value) => TryGetString(out value, Encoding.UTF8);
    public bool TryGetString(out byte[] value) => TryReadLengthPrefixedBytes(TagString, out value);

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

    public bool TryGetWide(out string value) => TryGetWString(out value);

    public bool TryGetRaw(Span<byte> destination)
    {
        if (destination.IsEmpty)
            return false;

        return ReadTag(TagRawBytes) && _buffer.Read(destination);
    }

    public void PutArray<T>(ReadOnlySpan<T> values, Action<NativePrimitiveSerializer, T> put)
    {
        ArgumentNullException.ThrowIfNull(put);

        WriteTag(TagArray);
        Put((uint)values.Length);

        foreach (var value in values)
            put(this, value);
    }

    public bool TryBeginArray(out uint count)
    {
        if (!ReadTag(TagArray) || !TryGet(out count))
        {
            count = default;
            return false;
        }

        return true;
    }

    public bool TryGetArray<T>(
        Span<T> destination,
        Func<NativePrimitiveSerializer, (bool Ok, T Value)> get,
        out int count)
    {
        ArgumentNullException.ThrowIfNull(get);
        count = 0;

        if (!ReadTag(TagArray) ||
            !TryGet(out uint itemCount) ||
            itemCount > (uint)destination.Length)
        {
            return false;
        }

        for (var i = 0; i < (int)itemCount; i++)
        {
            var result = get(this);
            if (!result.Ok)
                return false;

            destination[i] = result.Value;
        }

        count = (int)itemCount;
        return true;
    }

    internal void WriteCollectionTag(byte tag) => WriteTag(tag);
    internal bool ReadCollectionTag(byte tag) => ReadTag(tag);
    internal bool IsTaggingEnabled => _tagging;

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

        if (!ReadTag(tag) ||
            !TryGet(out uint size) ||
            size > int.MaxValue ||
            size > _buffer.ReadLength)
        {
            return false;
        }

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
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(bytes, value);
        _buffer.Write(bytes);
    }

    private void WriteUInt16(byte tag, ushort value)
    {
        WriteTag(tag);
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        _buffer.Write(bytes);
    }

    private void WriteUInt16LittleEndian(byte tag, ushort value)
    {
        WriteTag(tag);
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        _buffer.Write(bytes);
    }

    private void WriteInt32(byte tag, int value)
    {
        WriteTag(tag);
        WriteInt32Raw(value);
    }

    private void WriteUInt32(byte tag, uint value)
    {
        WriteTag(tag);
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        _buffer.Write(bytes);
    }

    private void WriteInt64(byte tag, long value)
    {
        WriteTag(tag);
        WriteInt64Raw(value);
    }

    private void WriteUInt64(byte tag, ulong value)
    {
        WriteTag(tag);
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        _buffer.Write(bytes);
    }

    private void WriteInt32Raw(int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        _buffer.Write(bytes);
    }

    private void WriteInt64Raw(long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        _buffer.Write(bytes);
    }

    private bool TryReadByte(byte tag, out byte value)
    {
        value = default;

        if (!ReadTag(tag))
            return false;

        Span<byte> bytes = stackalloc byte[1];
        if (!_buffer.Read(bytes))
            return false;

        value = bytes[0];
        return true;
    }

    private bool TryReadInt16(byte tag, out short value)
    {
        value = default;
        Span<byte> bytes = stackalloc byte[2];

        if (!ReadTag(tag) || !_buffer.Read(bytes))
            return false;

        value = BinaryPrimitives.ReadInt16BigEndian(bytes);
        return true;
    }

    private bool TryReadUInt16(byte tag, out ushort value)
    {
        value = default;
        Span<byte> bytes = stackalloc byte[2];

        if (!ReadTag(tag) || !_buffer.Read(bytes))
            return false;

        value = BinaryPrimitives.ReadUInt16BigEndian(bytes);
        return true;
    }

    private bool TryReadUInt16LittleEndian(byte tag, out ushort value)
    {
        value = default;
        Span<byte> bytes = stackalloc byte[2];

        if (!ReadTag(tag) || !_buffer.Read(bytes))
            return false;

        value = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        return true;
    }

    private bool TryReadInt32(byte tag, out int value)
    {
        value = default;
        Span<byte> bytes = stackalloc byte[4];

        if (!ReadTag(tag) || !_buffer.Read(bytes))
            return false;

        value = BinaryPrimitives.ReadInt32BigEndian(bytes);
        return true;
    }

    private bool TryReadUInt32(byte tag, out uint value)
    {
        value = default;
        Span<byte> bytes = stackalloc byte[4];

        if (!ReadTag(tag) || !_buffer.Read(bytes))
            return false;

        value = BinaryPrimitives.ReadUInt32BigEndian(bytes);
        return true;
    }

    private bool TryReadInt64(byte tag, out long value)
    {
        value = default;
        Span<byte> bytes = stackalloc byte[8];

        if (!ReadTag(tag) || !_buffer.Read(bytes))
            return false;

        value = BinaryPrimitives.ReadInt64BigEndian(bytes);
        return true;
    }

    private bool TryReadUInt64(byte tag, out ulong value)
    {
        value = default;
        Span<byte> bytes = stackalloc byte[8];

        if (!ReadTag(tag) || !_buffer.Read(bytes))
            return false;

        value = BinaryPrimitives.ReadUInt64BigEndian(bytes);
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
