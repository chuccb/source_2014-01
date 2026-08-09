using System.Buffers.Binary;

namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Primitive wire operations verified against KSerializer.cpp.</summary>
public sealed class NativePrimitiveSerializer(KSerBuffer buffer, bool tagging = false)
{
    private readonly KSerBuffer _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    private readonly bool _tagging = tagging;

    public void Put(sbyte value) => WriteByte(0, unchecked((byte)value));
    public void Put(byte value) => WriteByte(2, value);
    public void Put(short value) => WriteNumber(3, value, BinaryPrimitives.WriteInt16BigEndian);
    public void Put(ushort value) => WriteNumber(4, value, BinaryPrimitives.WriteUInt16BigEndian);
    public void Put(int value) => WriteNumber(5, value, BinaryPrimitives.WriteInt32BigEndian);
    public void Put(uint value) => WriteNumber(6, value, BinaryPrimitives.WriteUInt32BigEndian);
    public void Put(long value) => WriteNumber(7, value, BinaryPrimitives.WriteInt64BigEndian);
    public void Put(ulong value) => WriteNumber(8, value, BinaryPrimitives.WriteUInt64BigEndian);
    public void Put(float value) => Put(BitConverter.SingleToInt32Bits(value));
    public void Put(double value) => Put(BitConverter.DoubleToInt64Bits(value));
    public void Put(bool value) => WriteByte(11, (byte)(value ? 1 : 0));
    public void PutWChar(ushort value) => WriteNumber(1, value, BinaryPrimitives.WriteUInt16BigEndian);

    public void PutRaw(ReadOnlySpan<byte> value)
    {
        WriteTag(15);
        _buffer.Write(value);
    }

    private void WriteByte(byte tag, byte value)
    {
        WriteTag(tag);
        _buffer.Write([value]);
    }

    private void WriteNumber<T>(byte tag, T value, Action<Span<byte>, T> writer) where T : unmanaged
    {
        WriteTag(tag);
        Span<byte> bytes = stackalloc byte[8];
        writer(bytes, value);
        _buffer.Write(bytes[..System.Runtime.CompilerServices.Unsafe.SizeOf<T>()]);
    }

    private void WriteTag(byte tag)
    {
        if (_tagging)
            _buffer.Write([tag]);
    }
}
