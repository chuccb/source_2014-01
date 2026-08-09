using System.Runtime.InteropServices;
using System.Text;
using System.Buffers.Binary;

namespace KncWX2Server.Protocol;

public sealed class KSerializer
{
    private KSerBuffer? _buffer;
    private bool _tagsEnabled;

    public bool BeginWriting(KSerBuffer buffer, bool tagging = false) { _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer)); _tagsEnabled = tagging; return true; }
    public bool EndWriting() { _buffer = null; return true; }
    public bool BeginReading(KSerBuffer buffer, bool tagging = false) { _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer)); _tagsEnabled = tagging; return true; }
    public bool EndReading() { _buffer = null; return true; }
    public int ReadLength => _buffer?.ReadLength ?? 0;

    public bool Put(byte value) => WriteNumber(value, SerializeTag.UChar);
    public bool Get(out byte value) => ReadNumber(out value, SerializeTag.UChar);
    public bool Put(sbyte value) => WriteNumber(unchecked((byte)value), SerializeTag.Char);
    public bool Get(out sbyte value) { var ok = ReadNumber(out byte raw, SerializeTag.Char); value = unchecked((sbyte)raw); return ok; }
    public bool Put(short value) => WriteNumber(value, SerializeTag.Short);
    public bool Get(out short value) => ReadNumber(out value, SerializeTag.Short);
    public bool Put(ushort value) => WriteNumber(value, SerializeTag.UShort);
    public bool Get(out ushort value) => ReadNumber(out value, SerializeTag.UShort);
    public bool Put(int value) => WriteNumber(value, SerializeTag.Int);
    public bool Get(out int value) => ReadNumber(out value, SerializeTag.Int);
    public bool Put(uint value) => WriteNumber(value, SerializeTag.DWord);
    public bool Get(out uint value) => ReadNumber(out value, SerializeTag.DWord);
    public bool Put(long value) => WriteNumber(value, SerializeTag.Int64);
    public bool Get(out long value) => ReadNumber(out value, SerializeTag.Int64);
    public bool Put(ulong value) => WriteNumber(value, SerializeTag.UInt64);
    public bool Get(out ulong value) => ReadNumber(out value, SerializeTag.UInt64);
    public bool Put(float value) => WriteNumber(BitConverter.SingleToInt32Bits(value), SerializeTag.Float);
    public bool Get(out float value) { var ok = ReadNumber(out int raw, SerializeTag.Float); value = BitConverter.Int32BitsToSingle(raw); return ok; }
    public bool Put(double value) => WriteNumber(BitConverter.DoubleToInt64Bits(value), SerializeTag.Double);
    public bool Get(out double value) { var ok = ReadNumber(out long raw, SerializeTag.Double); value = BitConverter.Int64BitsToDouble(raw); return ok; }
    public bool Put(bool value) => WriteNumber((byte)(value ? 1 : 0), SerializeTag.Bool);
    public bool Get(out bool value) { var ok = ReadNumber(out byte raw, SerializeTag.Bool); value = raw == 1; return ok; }

    public bool Put(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!WriteTag(SerializeTag.String)) return false;
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        return Put((uint)bytes.Length) && (bytes.Length == 0 || WriteBytes(bytes));
    }

    public bool Get(out string value)
    {
        value = string.Empty;
        if (!ReadAndCheckTag(SerializeTag.String) || !Get(out uint size)) return false;
        if (size > ReadLength || size > int.MaxValue) return false;
        if (size == 0) return true;
        byte[] bytes = new byte[(int)size];
        if (!ReadBytes(bytes)) return false;
        value = Encoding.UTF8.GetString(bytes);
        return true;
    }

    public bool PutWide(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!WriteTag(SerializeTag.WString)) return false;
        byte[] bytes = Encoding.Unicode.GetBytes(value);
        return Put((uint)bytes.Length) && (bytes.Length == 0 || WriteBytes(bytes));
    }

    public bool GetWide(out string value)
    {
        value = string.Empty;
        if (!ReadAndCheckTag(SerializeTag.WString) || !Get(out uint size)) return false;
        if ((size & 1) != 0 || size > ReadLength || size > int.MaxValue) return false;
        if (size == 0) return true;
        byte[] bytes = new byte[(int)size];
        if (!ReadBytes(bytes)) return false;
        value = Encoding.Unicode.GetString(bytes);
        return true;
    }

    public bool PutRaw(ReadOnlySpan<byte> bytes) => !bytes.IsEmpty && WriteTag(SerializeTag.RawBytes) && WriteBytes(bytes);
    public bool GetRaw(Span<byte> bytes) => !bytes.IsEmpty && ReadAndCheckTag(SerializeTag.RawBytes) && ReadBytes(bytes);

    public bool Put(KSerBuffer value)
    {
        if (!WriteTag(SerializeTag.Buffer) || !Put((uint)value.Length)) return false;
        if (value.Length == 0) return true;
        return Put(value.IsCompressed) && PutRaw(value.Data.Span);
    }

    public bool Get(KSerBuffer value)
    {
        value.Clear();
        if (!ReadAndCheckTag(SerializeTag.Buffer) || !Get(out uint length)) return false;
        if (length == 0) return true;
        if (!Get(out bool compressed) || length > ReadLength || length > int.MaxValue) return false;
        byte[] data = new byte[(int)length];
        if (!GetRaw(data)) return false;
        value.Write(data);
        // Native Get restores m_bCompress but does not transparently decompress.
        if (compressed) value.MarkCompressed();
        return true;
    }

    public bool Put(KPerformerInfo value)
    {
        if (!WriteTag(SerializeTag.UserClass) || !Put(value.PerformerId)) return false;
        if (!WriteTag(SerializeTag.Set) || !Put((uint)value.UidListSize)) return false;
        foreach (long uid in value.UidList) if (!Put(uid)) return false;
        return true;
    }

    private bool WriteNumber<T>(T value, SerializeTag tag) where T : unmanaged
    {
        if (!WriteTag(tag)) return false;
        int size = Marshal.SizeOf<T>();
        Span<byte> bytes = stackalloc byte[8];
        MemoryMarshal.Write(bytes, in value);
        if (BitConverter.IsLittleEndian) bytes[..size].Reverse();
        return WriteBytes(bytes[..size]);
    }

    private bool ReadNumber<T>(out T value, SerializeTag tag) where T : unmanaged
    {
        value = default;
        if (!ReadAndCheckTag(tag)) return false;
        int size = Marshal.SizeOf<T>();
        Span<byte> bytes = stackalloc byte[8];
        if (!ReadBytes(bytes[..size])) return false;
        if (BitConverter.IsLittleEndian) bytes[..size].Reverse();
        value = MemoryMarshal.Read<T>(bytes);
        return true;
    }

    private bool WriteTag(SerializeTag tag) => !_tagsEnabled || WriteBytes([(byte)tag]);
    private bool ReadAndCheckTag(SerializeTag expected) { if (!_tagsEnabled) return true; Span<byte> raw = stackalloc byte[1]; return ReadBytes(raw) && raw[0] == (byte)expected; }
    private bool WriteBytes(ReadOnlySpan<byte> bytes) => _buffer is not null && !bytes.IsEmpty && _buffer.Write(bytes);
    private bool ReadBytes(Span<byte> bytes) => _buffer is not null && !bytes.IsEmpty && _buffer.Read(bytes);
}

public enum SerializeTag : byte
{
    Char, WChar, UChar, Short, UShort, Int, DWord, Int64, UInt64, Float, Double, Bool,
    String, WString, Array, RawBytes, Pair, Vector, List, Deque, Set, Multiset, Map, Multimap,
    Buffer, KeyedSerializer, UserClass,
}
