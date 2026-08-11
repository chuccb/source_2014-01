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
        TestTaggedSerializer();
        TestStringWireFormat();
        TestPerformerInfo();
        TestEventTraceAndClone();
        TestEventSerialization();
        TestEventFromTypeIsNotSerialized();
        MapSerializerCompatibilityTests.Run();
        KUnitInfoItemSerializerCompatibilityTests.Run();
        BadAttitudeCompatibilityTests.Run();
        CenterRuntimeCompatibilityTests.Run();
        CenterRoomCompatibilityTests.Run();
        BattleFieldDangerousCompatibilityTests.Run();
        BattleFieldRoomCompatibilityTests.Run();

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

        var changed = KncUid.SetServerId(uid, 0x12);
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
        var temp = KncUid.SetTempUid(value);

        Assert(KncUid.IsTemporary(temp));
        AssertEqual(value, temp & ~KncUid.TemporaryUidBit);

        var generated = KncUid.GetTempUid();
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
        Assert(serializer.Put((char)0x1234));
        serializer.EndWriting();

        Assert(buffer.Data.Span.SequenceEqual(
        [
            1, 2, 3, 4,
            1,
            0xA1, 0xB2, 0xC3, 0xD4,
            0x12, 0x34,
        ]));

        buffer.Reset();
        serializer.BeginReading(buffer);
        Assert(serializer.Get(out int intValue));
        AssertEqual(0x01020304, intValue);
        Assert(serializer.Get(out bool boolValue) && boolValue);
        Assert(serializer.Get(out uint uintValue));
        AssertEqual(unchecked((long)0xA1B2C3D4), uintValue);
        Assert(serializer.Get(out char charValue) && charValue == 'ሴ');
        serializer.EndReading();
    }

    private static void TestTaggedSerializer()
    {
        var tagged = new KSerBuffer();
        var serializer = new KSerializer();

        serializer.BeginWriting(tagged, tagging: true);
        Assert(serializer.Put((ushort)0x1234));
        Assert(serializer.Put((char)0x5678));
        Assert(serializer.Put(true));
        serializer.EndWriting();

        Assert(tagged.Data.Span.SequenceEqual([4, 0x12, 0x34, 1, 0x56, 0x78, 11, 1]));

        tagged.Reset();
        serializer.BeginReading(tagged, tagging: true);
        Assert(serializer.Get(out ushort value) && value == 0x1234);
        Assert(serializer.Get(out char character) && character == '噸');
        Assert(serializer.Get(out bool flag) && flag);
        serializer.EndReading();
    }

    private static void TestStringWireFormat()
    {
        var plain = new KSerBuffer();
        var serializer = new KSerializer();

        serializer.BeginWriting(plain);
        Assert(serializer.Put("ABC"));
        Assert(serializer.PutW("測試"));
        serializer.EndWriting();

        Assert(plain.Data.Span.SequenceEqual(
        [
            0, 0, 0, 3, 65, 66, 67,
            0, 0, 0, 4, 0x2C, 0x6E, 0x66, 0x8A,
        ]));

        plain.Reset();
        serializer.BeginReading(plain);
        Assert(serializer.Get(out string ascii) && ascii == "ABC");
        Assert(serializer.GetW(out string wide) && wide == "測試");
        serializer.EndReading();

        var tagged = new KSerBuffer();
        serializer.BeginWriting(tagged, tagging: true);
        Assert(serializer.Put("A"));
        serializer.EndWriting();
        Assert(tagged.Data.Span.SequenceEqual([12, 0, 0, 0, 1, 65]));
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
        var ev = new KEvent();
        ev.SetData(7, [100, -1], (ushort)SystemEventId.E_HEART_BEAT, [1, 2, 3]);
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

    private static void TestEventSerialization()
    {
        var original = new KEvent();
        original.SetData(77, [1001, 2002], (ushort)SystemEventId.E_LOG_NOT, [0xAA, 0xBB, 0xCC]);
        original.Destination.AddUid(11);
        original.Destination.AddUid(22);

        var buffer = new KSerBuffer();
        var serializer = new KSerializer();
        serializer.BeginWriting(buffer);
        Assert(serializer.PutEvent(original));
        serializer.EndWriting();

        buffer.Reset();
        var decoded = new KEvent();
        serializer.BeginReading(buffer);
        Assert(serializer.GetEvent(decoded));
        serializer.EndReading();

        AssertEqual(original.Destination.PerformerId, decoded.Destination.PerformerId);
        AssertEqual(original.GetFirstSenderUid(), decoded.GetFirstSenderUid());
        AssertEqual(original.GetLastSenderUid(), decoded.GetLastSenderUid());
        AssertEqual(original.EventId, decoded.EventId);
        Assert(decoded.Destination.FindUid(11));
        Assert(decoded.Destination.FindUid(22));
        Assert(decoded.Buffer.Data.Span.SequenceEqual([0xAA, 0xBB, 0xCC]));
    }

    private static void TestEventFromTypeIsNotSerialized()
    {
        var original = new KEvent();
        original.SetData(1, [2, 3], (ushort)SystemEventId.E_HEART_BEAT, [9]);
        original.SetFromType(KEvent.EventFromType.Client);

        var buffer = new KSerBuffer();
        var serializer = new KSerializer();
        serializer.BeginWriting(buffer);
        Assert(serializer.PutEvent(original));
        serializer.EndWriting();

        buffer.Reset();
        var decoded = new KEvent();
        serializer.BeginReading(buffer);
        Assert(serializer.GetEvent(decoded));
        serializer.EndReading();

        Assert(decoded.FromType == KEvent.EventFromType.None);
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("compatibility assertion failed");
        }
    }

    private static void AssertEqual(long expected, long actual)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"expected 0x{expected:X16}, actual 0x{actual:X16}");
        }
    }
}
