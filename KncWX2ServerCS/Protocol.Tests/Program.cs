using KncWX2Server.Protocol;

static class Program
{
    static void Main()
    {
        TestLegacyLayout();
        TestExtendedLayout();
        TestTempBit();
        TestSerBuffer();
        TestSerializerWireFormat();
        TestPerformerInfo();
        TestEventTraceAndClone();
        Console.WriteLine("KncWX2Server protocol compatibility vectors: PASS");
    }

    private static void TestLegacyLayout()
    {
        const long pure = 0x000000123456789A;
        const long group = 0x2A;
        const long server = 0x7B;
        const long reserved = 0xC4;
        long uid = 0;
        uid = KncUid.SetPureUid(uid, pure);
        uid = KncUid.SetServerGroupId(uid, group);
        uid = KncUid.SetServerId(uid, server);
        uid = KncUid.SetReservedId(uid, reserved);
        AssertEqual(pure, KncUid.ExtractPureUid(uid));
        AssertEqual(group, KncUid.ExtractServerGroupId(uid));
        AssertEqual(server, KncUid.ExtractServerId(uid));
        AssertEqual(reserved, KncUid.ExtractReservedId(uid));
        AssertEqual((reserved << 8) | server, KncUid.ExtractCodeId(uid));
        long changed = KncUid.SetServerId(uid, 0x12);
        AssertEqual(pure, KncUid.ExtractPureUid(changed));
        AssertEqual(group, KncUid.ExtractServerGroupId(changed));
        AssertEqual(0x12, KncUid.ExtractServerId(changed));
        AssertEqual(reserved, KncUid.ExtractReservedId(changed));
    }

    private static void TestExtendedLayout()
    {
        const long pure = 0x0000000ABCDEF123;
        const long group = 0x155;
        const long server = 0x7B;
        const long reserved = 0xC4;
        long uid = 0;
        uid = KncUid.SetPureUid(uid, pure, KncUid.Layout.ExtendedServerGroup);
        uid = KncUid.SetServerGroupId(uid, group, KncUid.Layout.ExtendedServerGroup);
        uid = KncUid.SetServerId(uid, server, KncUid.Layout.ExtendedServerGroup);
        uid = KncUid.SetReservedId(uid, reserved, KncUid.Layout.ExtendedServerGroup);
        AssertEqual(pure, KncUid.ExtractPureUid(uid, KncUid.Layout.ExtendedServerGroup));
        AssertEqual(group, KncUid.ExtractServerGroupId(uid, KncUid.Layout.ExtendedServerGroup));
        AssertEqual(server, KncUid.ExtractServerId(uid, KncUid.Layout.ExtendedServerGroup));
        AssertEqual(reserved, KncUid.ExtractReservedId(uid, KncUid.Layout.ExtendedServerGroup));
        AssertEqual((reserved << 8) | server, KncUid.ExtractCodeId(uid, KncUid.Layout.ExtendedServerGroup));
    }

    private static void TestTempBit()
    {
        const long value = 0x123456789A;
        long temp = KncUid.SetTempUid(value);
        Assert(KncUid.IsTemporary(temp));
        AssertEqual(value, temp & ~KncUid.TemporaryUidBit);
        long generated = KncUid.GetTempUid();
        Assert(KncUid.IsTemporary(generated));
        Assert((generated & KncUid.SignBitMask) == 0);
        Assert(KncUid.ExtractPureUid(generated) != 0);
    }

    private static void TestSerBuffer()
    {
        var buffer = new KSerBuffer();
        byte[] source = [0, 1, 2, 0x7F, 0x80, 0xFF];
        Assert(buffer.Write(source));
        Span<byte> first = stackalloc byte[2];
        Assert(buffer.Read(first));
        AssertEqual(2, buffer.ReadLength);
        buffer.Reset();
        var clone = buffer.Clone();
        Assert(buffer.Equals(clone));
        Assert(buffer.Compress());
        Assert(buffer.IsCompressed);
        Assert(buffer.UnCompress());
        Assert(!buffer.IsCompressed);
        Assert(buffer.Equals(clone));
    }

    private static void TestSerializerWireFormat()
    {
        var buffer = new KSerBuffer();
        var serializer = new KSerializer();
        serializer.BeginWriting(buffer);
        Assert(serializer.Put(0x01020304));
        Assert(serializer.Put(true));
        Assert(serializer.Put((uint)0xA1B2C3D4));
        serializer.EndWriting();
        Assert(buffer.Data.Span.SequenceEqual([1, 2, 3, 4, 1, 0xA1, 0xB2, 0xC3, 0xD4]));

        buffer.Reset();
        serializer.BeginReading(buffer);
        Assert(serializer.Get(out int intValue));
        AssertEqual(0x01020304, intValue);
        Assert(serializer.Get(out bool boolValue) && boolValue);
        Assert(serializer.Get(out uint uintValue));
        AssertEqual(unchecked((long)0xA1B2C3D4), uintValue);
        serializer.EndReading();

        var tagged = new KSerBuffer();
        serializer.BeginWriting(tagged, tagging: true);
        Assert(serializer.Put((ushort)0x1234));
        serializer.EndWriting();
        Assert(tagged.Data.Span.SequenceEqual([4, 0x12, 0x34]));
    }

    private static void TestPerformerInfo()
    {
        var performer = new KPerformerInfo { PerformerId = 123 };
        Assert(performer.AddUid(20));
        Assert(performer.AddUid(10));
        Assert(performer.FindUid(10));
        AssertEqual(10, performer.GetFirstUid());
        AssertEqual(2, performer.UidListSize);
        var clone = performer.Clone();
        AssertEqual(123, clone.PerformerId);
        Assert(clone.FindUid(20));
    }

    private static void TestEventTraceAndClone()
    {
        var trace = new long[] { 100, -1 };
        var ev = new KEvent();
        ev.SetData(7, trace, (ushort)SystemEventId.E_HEART_BEAT, [1, 2, 3]);
        AssertEqual(100, ev.GetFirstSenderUid());
        AssertEqual(100, ev.GetLastSenderUid());
        ev.PushTrace(200);
        AssertEqual(200, ev.GetLastSenderUid());
        ev.PopTrace();
        AssertEqual(100, ev.GetLastSenderUid());
        AssertEqual("E_HEART_BEAT", ev.GetIdString());
        var clone = ev.Clone();
        AssertEqual(ev.EventId, clone.EventId);
        AssertEqual(ev.GetFirstSenderUid(), clone.GetFirstSenderUid());
        Assert(clone.Buffer.Data.Span.SequenceEqual([1, 2, 3]));
    }

    private static void Assert(bool condition)
    {
        if (!condition) throw new InvalidOperationException("compatibility assertion failed");
    }

    private static void AssertEqual(long expected, long actual)
    {
        if (expected != actual) throw new InvalidOperationException($"expected 0x{expected:X16}, actual 0x{actual:X16}");
    }
}
