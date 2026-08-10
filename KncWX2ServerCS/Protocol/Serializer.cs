using System.Runtime.InteropServices;
using System.Text;

namespace KncWX2Server.Protocol;

/// <summary>Compatibility serializer for the native KSerializer wire format.</summary>
/// <remarks>
/// The native implementation serializes numeric values in network byte order, prefixes
/// values with one-byte type tags only when tagging is enabled, and stores string lengths
/// as DWORD byte counts. See KNCSDK/Include/Serializer/Serializer.cpp.
/// </remarks>
public sealed class KSerializer
{
    private KSerBuffer? _buffer;
    private bool _tagsEnabled;

    public bool BeginWriting(KSerBuffer buffer, bool tagging = false)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _tagsEnabled = tagging;
        return true;
    }

    public bool EndWriting()
    {
        _buffer = null;
        return true;
    }

    public bool BeginReading(KSerBuffer buffer, bool tagging = false)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _tagsEnabled = tagging;
        return true;
    }

    public bool EndReading()
    {
        _buffer = null;
        return true;
    }

    public int ReadLength => _buffer?.ReadLength ?? 0;

    public bool Put(byte value) => WriteNumber(value, SerializeTag.UChar);
    public bool Get(out byte value) => ReadNumber(out value, SerializeTag.UChar);

    public bool Put(sbyte value) => WriteNumber(unchecked((byte)value), SerializeTag.Char);

    public bool Get(out sbyte value)
    {
        var ok = ReadNumber(out byte raw, SerializeTag.Char);
        value = unchecked((sbyte)raw);
        return ok;
    }

    public bool Put(char value) => WriteNumber((ushort)value, SerializeTag.WChar);

    public bool Get(out char value)
    {
        var ok = ReadNumber(out ushort raw, SerializeTag.WChar);
        value = (char)raw;
        return ok;
    }

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

    public bool Get(out float value)
    {
        var ok = ReadNumber(out int raw, SerializeTag.Float);
        value = BitConverter.Int32BitsToSingle(raw);
        return ok;
    }

    public bool Put(double value) => WriteNumber(BitConverter.DoubleToInt64Bits(value), SerializeTag.Double);

    public bool Get(out double value)
    {
        var ok = ReadNumber(out long raw, SerializeTag.Double);
        value = BitConverter.Int64BitsToDouble(raw);
        return ok;
    }

    public bool Put(bool value) => WriteNumber((byte)(value ? 1 : 0), SerializeTag.Bool);

    public bool Get(out bool value)
    {
        var ok = ReadNumber(out byte raw, SerializeTag.Bool);
        value = raw == 1;
        return ok;
    }

    public bool Put(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!WriteTag(SerializeTag.String))
        {
            return false;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        return Put((uint)bytes.Length) && (bytes.Length == 0 || WriteBytes(bytes));
    }

    public bool Get(out string value)
    {
        value = string.Empty;

        if (!ReadAndCheckTag(SerializeTag.String) ||
            !Get(out uint length) ||
            length > ReadLength ||
            length > int.MaxValue)
        {
            return false;
        }

        if (length == 0)
        {
            return true;
        }

        var bytes = new byte[(int)length];
        if (!ReadBytes(bytes))
        {
            return false;
        }

        value = Encoding.UTF8.GetString(bytes);
        return true;
    }

    public bool PutW(string value) => PutWide(value);
    public bool GetW(out string value) => GetWide(out value);

    public bool PutWide(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!WriteTag(SerializeTag.WString))
        {
            return false;
        }

        var bytes = Encoding.Unicode.GetBytes(value);
        return Put((uint)bytes.Length) && (bytes.Length == 0 || WriteBytes(bytes));
    }

    public bool GetWide(out string value)
    {
        value = string.Empty;

        if (!ReadAndCheckTag(SerializeTag.WString) ||
            !Get(out uint length) ||
            (length & 1) != 0 ||
            length > ReadLength ||
            length > int.MaxValue)
        {
            return false;
        }

        if (length == 0)
        {
            return true;
        }

        var bytes = new byte[(int)length];
        if (!ReadBytes(bytes))
        {
            return false;
        }

        value = Encoding.Unicode.GetString(bytes);
        return true;
    }

    public bool PutRaw(ReadOnlySpan<byte> bytes) =>
        !bytes.IsEmpty && WriteTag(SerializeTag.RawBytes) && WriteBytes(bytes);

    public bool GetRaw(Span<byte> bytes) =>
        !bytes.IsEmpty && ReadAndCheckTag(SerializeTag.RawBytes) && ReadBytes(bytes);

    public bool Put(KSerBuffer value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!WriteTag(SerializeTag.Buffer) || !Put((uint)value.Length))
        {
            return false;
        }

        return value.Length == 0 || Put(value.IsCompressed) && PutRaw(value.Data.Span);
    }

    public bool Get(KSerBuffer value)
    {
        ArgumentNullException.ThrowIfNull(value);
        value.Clear();

        if (!ReadAndCheckTag(SerializeTag.Buffer) || !Get(out uint length))
        {
            return false;
        }

        if (length == 0)
        {
            return true;
        }

        if (!Get(out bool compressed) || length > ReadLength || length > int.MaxValue)
        {
            return false;
        }

        var bytes = new byte[(int)length];
        if (!GetRaw(bytes))
        {
            return false;
        }

        value.Write(bytes);
        if (compressed)
        {
            value.MarkCompressed();
        }

        return true;
    }

    public bool Put(KPerformerInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!WriteTag(SerializeTag.UserClass) ||
            !Put(value.PerformerId) ||
            !WriteTag(SerializeTag.Set) ||
            !Put((uint)value.UidListSize))
        {
            return false;
        }

        foreach (var uid in value.UidList)
        {
            if (!Put(uid))
            {
                return false;
            }
        }

        return true;
    }

    public bool Get(KPerformerInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!ReadAndCheckTag(SerializeTag.UserClass) ||
            !Get(out uint performerId) ||
            !ReadAndCheckTag(SerializeTag.Set) ||
            !Get(out uint count) ||
            count > KPerformerInfo.MaxUidNum)
        {
            return false;
        }

        value.ClearUids();
        for (uint i = 0; i < count; i++)
        {
            if (!Get(out long uid))
            {
                return false;
            }

            value.AddUid(uid);
        }

        value.PerformerId = performerId;
        return true;
    }

    /// <summary>Native STL vector helper: container tag, DWORD count, then each element.</summary>
    public bool PutVector<T>(IReadOnlyList<T> values, Func<KSerializer, T, bool> put) =>
        PutContainer(SerializeTag.Vector, values, put);

    public bool GetVector<T>(ICollection<T> values, Func<KSerializer, (bool Ok, T Value)> get)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(get);

        if (!ReadAndCheckTag(SerializeTag.Vector) || !Get(out uint count))
        {
            return false;
        }

        values.Clear();
        for (uint i = 0; i < count; i++)
        {
            var result = get(this);
            if (!result.Ok)
            {
                return false;
            }

            values.Add(result.Value);
        }

        return true;
    }

    public bool PutList<T>(IReadOnlyList<T> values, Func<KSerializer, T, bool> put) =>
        PutContainer(SerializeTag.List, values, put);

    public bool PutDeque<T>(IReadOnlyList<T> values, Func<KSerializer, T, bool> put) =>
        PutContainer(SerializeTag.Deque, values, put);

    public bool PutSet<T>(IEnumerable<T> values, Func<KSerializer, T, bool> put)
    {
        ArgumentNullException.ThrowIfNull(values);
        return PutContainer(SerializeTag.Set, values.ToArray(), put);
    }

    /// <summary>Native STL map helper: map tag, DWORD count, then key/value pairs without pair tags.</summary>
    public bool PutMap<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> values,
        Func<KSerializer, TKey, bool> putKey,
        Func<KSerializer, TValue, bool> putValue)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(putKey);
        ArgumentNullException.ThrowIfNull(putValue);

        if (!WriteTag(SerializeTag.Map) || !Put((uint)values.Count))
        {
            return false;
        }

        foreach (var pair in values)
        {
            if (!putKey(this, pair.Key) || !putValue(this, pair.Value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Native STL map reader: clear the destination, then insert each decoded key/value pair.</summary>
    public bool GetMap<TKey, TValue>(
        IDictionary<TKey, TValue> values,
        Func<KSerializer, (bool Ok, TKey Value)> getKey,
        Func<KSerializer, (bool Ok, TValue Value)> getValue)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(getKey);
        ArgumentNullException.ThrowIfNull(getValue);

        if (!ReadAndCheckTag(SerializeTag.Map) || !Get(out uint count))
        {
            return false;
        }

        values.Clear();
        for (uint i = 0; i < count; i++)
        {
            var key = getKey(this);
            if (!key.Ok)
            {
                return false;
            }

            var value = getValue(this);
            if (!value.Ok)
            {
                return false;
            }

            values[key.Value] = value.Value;
        }

        return true;
    }

    public bool PutPair<T1, T2>(
        T1 first,
        T2 second,
        Func<KSerializer, T1, bool> putFirst,
        Func<KSerializer, T2, bool> putSecond) =>
        WriteTag(SerializeTag.Pair) && putFirst(this, first) && putSecond(this, second);

    public bool GetPair<T1, T2>(
        out T1 first,
        out T2 second,
        Func<KSerializer, (bool Ok, T1 Value)> getFirst,
        Func<KSerializer, (bool Ok, T2 Value)> getSecond)
    {
        first = default!;
        second = default!;

        if (!ReadAndCheckTag(SerializeTag.Pair))
        {
            return false;
        }

        var firstResult = getFirst(this);
        if (!firstResult.Ok)
        {
            return false;
        }

        var secondResult = getSecond(this);
        if (!secondResult.Ok)
        {
            return false;
        }

        first = firstResult.Value;
        second = secondResult.Value;
        return true;
    }

    public bool PutEvent(KEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return WriteTag(SerializeTag.UserClass) &&
               Put(value.Destination) &&
               Put(value.FirstTrace) &&
               Put(value.LastTrace) &&
               Put(value.EventId) &&
               Put(value.Buffer);
    }

    public bool GetEvent(KEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!ReadAndCheckTag(SerializeTag.UserClass) ||
            !Get(value.Destination) ||
            !Get(out long firstTrace) ||
            !Get(out long lastTrace) ||
            !Get(out ushort eventId) ||
            !Get(value.Buffer))
        {
            return false;
        }

        value.SetData(value.Destination.PerformerId, [firstTrace, lastTrace], eventId);
        return true;
    }

    private bool PutContainer<T>(
        SerializeTag tag,
        IReadOnlyList<T> values,
        Func<KSerializer, T, bool> put)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(put);

        if (!WriteTag(tag) || !Put((uint)values.Count))
        {
            return false;
        }

        foreach (var value in values)
        {
            if (!put(this, value))
            {
                return false;
            }
        }

        return true;
    }

    private bool WriteNumber<T>(T value, SerializeTag tag) where T : unmanaged
    {
        if (!WriteTag(tag))
        {
            return false;
        }

        var size = Marshal.SizeOf<T>();
        Span<byte> bytes = stackalloc byte[8];
        MemoryMarshal.Write(bytes, in value);

        if (BitConverter.IsLittleEndian)
        {
            bytes[..size].Reverse();
        }

        return WriteBytes(bytes[..size]);
    }

    private bool ReadNumber<T>(out T value, SerializeTag tag) where T : unmanaged
    {
        value = default;

        if (!ReadAndCheckTag(tag))
        {
            return false;
        }

        var size = Marshal.SizeOf<T>();
        Span<byte> bytes = stackalloc byte[8];
        if (!ReadBytes(bytes[..size]))
        {
            return false;
        }

        if (BitConverter.IsLittleEndian)
        {
            bytes[..size].Reverse();
        }

        value = MemoryMarshal.Read<T>(bytes);
        return true;
    }

    private bool WriteTag(SerializeTag tag) =>
        !_tagsEnabled || WriteBytes([(byte)tag]);

    private bool ReadAndCheckTag(SerializeTag expected)
    {
        if (!_tagsEnabled)
        {
            return true;
        }

        Span<byte> bytes = stackalloc byte[1];
        return ReadBytes(bytes) && bytes[0] == (byte)expected;
    }

    private bool WriteBytes(ReadOnlySpan<byte> bytes) =>
        _buffer is not null && !bytes.IsEmpty && _buffer.Write(bytes);

    private bool ReadBytes(Span<byte> bytes) =>
        _buffer is not null && !bytes.IsEmpty && _buffer.Read(bytes);
}

public enum SerializeTag : byte
{
    Char,
    WChar,
    UChar,
    Short,
    UShort,
    Int,
    DWord,
    Int64,
    UInt64,
    Float,
    Double,
    Bool,
    String,
    WString,
    Array,
    RawBytes,
    Pair,
    Vector,
    List,
    Deque,
    Set,
    Multiset,
    Map,
    Multimap,
    Buffer,
    KeyedSerializer,
    UserClass,
}