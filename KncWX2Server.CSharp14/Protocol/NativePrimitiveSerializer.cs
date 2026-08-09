using System.Buffers.Binary;

namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Primitive wire operations verified against native KSerializer.</summary>
public sealed class NativePrimitiveSerializer(KSerBuffer buffer, bool tagging = false)
{
    private readonly KSerBuffer _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    private readonly bool _tagging = tagging;

    public void Put(sbyte value) => WriteByte(0, unchecked((byte)value));
    public void PutWChar(ushort value) => WriteUInt16(1, value);
    public void Put(byte value) => WriteByte(2, value);
    public void Put(short value) => WriteInt16(3, value);
    public void Put(ushort value) => WriteUInt16(4, value);
    public void Put(int value) => WriteInt32(5, value);
    public void Put(uint value) => WriteUInt32(6, value);
    public void Put(long value) => WriteInt64(7, value);
    public void Put(ulong value) => WriteUInt64(8, value);
    public void Put(float value) { WriteTag(9); WriteInt32Raw(BitConverter.SingleToInt32Bits(value)); }
    public void Put(double value) { WriteTag(10); WriteInt64Raw(BitConverter.DoubleToInt64Bits(value)); }
    public void Put(bool value) => WriteByte(11, (byte)(value ? 1 : 0));

    public void PutRaw(ReadOnlySpan<byte> value)
    {
        WriteTag(15);
        _buffer.Write(value);
    }

    private void WriteByte(byte tag, byte value) { WriteTag(tag); _buffer.Write([value]); }
    private void WriteInt16(byte tag, short value) { WriteTag(tag); Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteInt16BigEndian(b, value); _buffer.Write(b); }
    private void WriteUInt16(byte tag, ushort value) { WriteTag(tag); Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16BigEndian(b, value); _buffer.Write(b); }
    private void WriteInt32(byte tag, int value) { WriteTag(tag); WriteInt32Raw(value); }
    private void WriteUInt32(byte tag, uint value) { WriteTag(tag); Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, value); _buffer.Write(b); }
    private void WriteInt64(byte tag, long value) { WriteTag(tag); WriteInt64Raw(value); }
    private void WriteUInt64(byte tag, ulong value) { WriteTag(tag); Span<byte> b = stackalloc byte[8]; BinaryPrimitives.WriteUInt64BigEndian(b, value); _buffer.Write(b); }
    private void WriteInt32Raw(int value) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteInt32BigEndian(b, value); _buffer.Write(b); }
    private void WriteInt64Raw(long value) { Span<byte> b = stackalloc byte[8]; BinaryPrimitives.WriteInt64BigEndian(b, value); _buffer.Write(b); }
    private void WriteTag(byte tag) { if (_tagging) _buffer.Write([tag]); }
}
