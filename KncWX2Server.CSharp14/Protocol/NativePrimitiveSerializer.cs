using System.Buffers.Binary;
using System.Text;

namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Managed implementation of the builtin portion of the native KSerializer.</summary>
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

    private const byte TagChar = 0, TagWChar = 1, TagUChar = 2, TagShort = 3, TagUShort = 4;
    private const byte TagInt = 5, TagDword = 6, TagInt64 = 7, TagUInt64 = 8, TagFloat = 9;
    private const byte TagDouble = 10, TagBool = 11, TagString = 12, TagWString = 13;
    private const byte TagArray = 14, TagRawBytes = 15;

    private readonly KSerBuffer _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    private readonly bool _tagging = tagging;
    public int ReadLength => _buffer.ReadLength;

    public void Put(sbyte value) => WriteByte(TagChar, unchecked((byte)value));
    // Native wchar_t is written by WriteNumber without HTON conversion: MSVC wchar_t is little-endian UTF-16.
    public void PutWChar(ushort value) => WriteUInt16LittleEndian(TagWChar, value);
    public void Put(byte value) => WriteByte(TagUChar, value);
    public void Put(short value) => WriteInt16(TagShort, value);
    public void Put(ushort value) => WriteUInt16(TagUShort, value);
    public void Put(int value) => WriteInt32(TagInt, value);
    public void Put(uint value) => WriteUInt32(TagDword, value);
    public void Put(long value) => WriteInt64(TagInt64, value);
    public void Put(ulong value) => WriteUInt64(TagUInt64, value);
    public void Put(float value) { WriteTag(TagFloat); WriteInt32Raw(BitConverter.SingleToInt32Bits(value)); }
    public void Put(double value) { WriteTag(TagDouble); WriteInt64Raw(BitConverter.DoubleToInt64Bits(value)); }
    public void Put(bool value) => WriteByte(TagBool, (byte)(value ? 1 : 0));

    public void Put(ReadOnlySpan<byte> value) => WriteLengthPrefixedBytes(TagString, value);
    public void Put(string value, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(value); ArgumentNullException.ThrowIfNull(encoding);
        Put(encoding.GetBytes(value));
    }
    public void PutCString(string value, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(value); ArgumentNullException.ThrowIfNull(encoding);
        var nul = value.IndexOf('\0'); Put(value[..(nul < 0 ? value.Length : nul)], encoding);
    }
    public void PutWString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        WriteLengthPrefixedBytes(TagWString, Encoding.Unicode.GetBytes(value));
    }
    public void PutWCString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var nul = value.IndexOf('\0'); PutWString(value[..(nul < 0 ? value.Length : nul)]);
    }
    public void PutRaw(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty) throw new ArgumentOutOfRangeException(nameof(value), "Native PutRaw requires len > 0.");
        WriteTag(TagRawBytes); _buffer.Write(value);
    }

    public bool TryGet(out sbyte value)
    {
        if (!TryReadByte(TagChar, out var raw)) { value = default; return false; }
        value = unchecked((sbyte)raw); return true;
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
        if (!TryReadInt32(TagFloat, out var raw)) { value = default; return false; }
        value = BitConverter.Int32BitsToSingle(raw); return true;
    }
    public bool TryGet(out double value)
    {
        if (!TryReadInt64(TagDouble, out var raw)) { value = default; return false; }
        value = BitConverter.Int64BitsToDouble(raw); return true;
    }
    public bool TryGet(out bool value)
    {
        if (!TryReadByte(TagBool, out var raw)) { value = default; return false; }
        value = raw == 1; return true;
    }
    public bool TryGetString(out byte[] value) => TryReadLengthPrefixedBytes(TagString, out value);
    public bool TryGetString(out string value, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);
        if (!TryGetString(out var bytes)) { value = string.Empty; return false; }
        value = encoding.GetString(bytes); return true;
    }
    public bool TryGetWString(out string value)
    {
        if (!TryReadLengthPrefixedBytes(TagWString, out var bytes) || (bytes.Length & 1) != 0) { value = string.Empty; return false; }
        value = Encoding.Unicode.GetString(bytes); return true;
    }
    public bool TryGetRaw(Span<byte> destination)
    {
        if (destination.IsEmpty) return false;
        return ReadTag(TagRawBytes) && _buffer.Read(destination);
    }
    public void PutArray<T>(ReadOnlySpan<T> values, Action<NativePrimitiveSerializer, T> put)
    {
        ArgumentNullException.ThrowIfNull(put); WriteTag(TagArray); Put((uint)values.Length);
        foreach (var value in values) put(this, value);
    }
    public bool TryBeginArray(out uint count)
    {
        if (!ReadTag(TagArray) || !TryGet(out count)) { count = default; return false; }
        return true;
    }

    internal void WriteCollectionTag(byte tag) => WriteTag(tag);
    internal bool ReadCollectionTag(byte tag) => ReadTag(tag);
    internal bool IsTaggingEnabled => _tagging;

    private void WriteLengthPrefixedBytes(byte tag, ReadOnlySpan<byte> bytes)
    {
        WriteTag(tag); Put((uint)bytes.Length); if (!bytes.IsEmpty) _buffer.Write(bytes);
    }
    private bool TryReadLengthPrefixedBytes(byte tag, out byte[] bytes)
    {
        bytes = [];
        if (!ReadTag(tag) || !TryGet(out uint size) || size > int.MaxValue || size > _buffer.ReadLength) return false;
        bytes = GC.AllocateUninitializedArray<byte>((int)size); return size == 0 || _buffer.Read(bytes);
    }
    private void WriteByte(byte tag, byte value) { WriteTag(tag); _buffer.Write([value]); }
    private void WriteInt16(byte tag, short value) { WriteTag(tag); Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteInt16BigEndian(b, value); _buffer.Write(b); }
    private void WriteUInt16(byte tag, ushort value) { WriteTag(tag); Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16BigEndian(b, value); _buffer.Write(b); }
    private void WriteUInt16LittleEndian(byte tag, ushort value) { WriteTag(tag); Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16LittleEndian(b, value); _buffer.Write(b); }
    private void WriteInt32(byte tag, int value) { WriteTag(tag); WriteInt32Raw(value); }
    private void WriteUInt32(byte tag, uint value) { WriteTag(tag); Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, value); _buffer.Write(b); }
    private void WriteInt64(byte tag, long value) { WriteTag(tag); WriteInt64Raw(value); }
    private void WriteUInt64(byte tag, ulong value) { WriteTag(tag); Span<byte> b = stackalloc byte[8]; BinaryPrimitives.WriteUInt64BigEndian(b, value); _buffer.Write(b); }
    private void WriteInt32Raw(int value) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteInt32BigEndian(b, value); _buffer.Write(b); }
    private void WriteInt64Raw(long value) { Span<byte> b = stackalloc byte[8]; BinaryPrimitives.WriteInt64BigEndian(b, value); _buffer.Write(b); }
    private bool TryReadByte(byte tag, out byte value) { value = default; if (!ReadTag(tag)) return false; Span<byte> b = stackalloc byte[1]; if (!_buffer.Read(b)) return false; value = b[0]; return true; }
    private bool TryReadInt16(byte tag, out short value) { value = default; Span<byte> b = stackalloc byte[2]; if (!ReadTag(tag) || !_buffer.Read(b)) return false; value = BinaryPrimitives.ReadInt16BigEndian(b); return true; }
    private bool TryReadUInt16(byte tag, out ushort value) { value = default; Span<byte> b = stackalloc byte[2]; if (!ReadTag(tag) || !_buffer.Read(b)) return false; value = BinaryPrimitives.ReadUInt16BigEndian(b); return true; }
    private bool TryReadUInt16LittleEndian(byte tag, out ushort value) { value = default; Span<byte> b = stackalloc byte[2]; if (!ReadTag(tag) || !_buffer.Read(b)) return false; value = BinaryPrimitives.ReadUInt16LittleEndian(b); return true; }
    private bool TryReadInt32(byte tag, out int value) { value = default; Span<byte> b = stackalloc byte[4]; if (!ReadTag(tag) || !_buffer.Read(b)) return false; value = BinaryPrimitives.ReadInt32BigEndian(b); return true; }
    private bool TryReadUInt32(byte tag, out uint value) { value = default; Span<byte> b = stackalloc byte[4]; if (!ReadTag(tag) || !_buffer.Read(b)) return false; value = BinaryPrimitives.ReadUInt32BigEndian(b); return true; }
    private bool TryReadInt64(byte tag, out long value) { value = default; Span<byte> b = stackalloc byte[8]; if (!ReadTag(tag) || !_buffer.Read(b)) return false; value = BinaryPrimitives.ReadInt64BigEndian(b); return true; }
    private bool TryReadUInt64(byte tag, out ulong value) { value = default; Span<byte> b = stackalloc byte[8]; if (!ReadTag(tag) || !_buffer.Read(b)) return false; value = BinaryPrimitives.ReadUInt64BigEndian(b); return true; }
    private bool ReadTag(byte expectedTag) { if (!_tagging) return true; Span<byte> tag = stackalloc byte[1]; return _buffer.Read(tag) && tag[0] == expectedTag; }
    private void WriteTag(byte tag) { if (_tagging) _buffer.Write([tag]); }
}
