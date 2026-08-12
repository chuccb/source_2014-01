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
        EventIdCompatibilityTests.Run();
        EventWireCompatibilityTests.Run();
        MapSerializerCompatibilityTests.Run();
        KUnitInfoItemSerializerCompatibilityTests.Run();
        BadAttitudeCompatibilityTests.Run();
        CenterRuntimeCompatibilityTests.Run();
        CenterRoomCompatibilityTests.Run();
        BattleFieldDangerousCompatibilityTests.Run();
        BattleFieldRoomCompatibilityTests.Run();
        BattleFieldRoomCatalogCompatibilityTests.Run();
        BattleFieldMiddleBossCompatibilityTests.Run();
        BattleFieldEventMonsterCompatibilityTests.Run();
        BattleFieldRoomStateMachineCompatibilityTests.Run();
        BattleFieldRoomControllerCompatibilityTests.Run();

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
        AssertEqual((server << 8) | reserved, KncUid.ExtractCodeId(uid));

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
        AssertEqual((server << 8) | reserved, KncUid.ExtractCodeId(uid, KncUid.Layout.ExtendedServerGroup));
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
        AssertEqual(4, buffer.ReadLength);

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
        Assert(tagged.Data.Span.SequenceEqual([12, 6, 0, 0, 0, 1, 65]));
    }

    private static void TestPerformerInfo()
    {
        var performer = new KPerformerInfo { PerformerId = 123 };
        Assert(performer.AddUid(20));
        Assert(performer.AddUid(10));
        Assert(performer.FindUid(10));
        AssertEqual(10L, performer.GetFirstUid());
        AssertEqual(2, performer.UidListSize);

        var clone = performer.Clone();
        AssertEqual((uint)123, clone.PerformerId);
        Assert(clone.FindUid(20));
    }

    private static void TestEventTraceAndClone()
    {
        var ev = new KEvent();
        ev.SetData(7, [100, -1], (ushort)SystemEventId.E_HEART_BEAT, [1, 2, 3]);
        AssertEqual(100L, ev.GetFirstSenderUid());
        AssertEqual(100L, ev.GetLastSenderUid());

        ev.PushTrace(200);
        AssertEqual(200L, ev.GetLastSenderUid());
        ev.PopTrace();
        AssertEqual(100L, ev.GetLastSenderUid());
        Assert(ev.GetIdString() == "E_HEART_BEAT");

        var clone = ev.Clone();
        AssertEqual(ev.EventId, clone.EventId);
        AssertEqual(ev.GetFirstSenderUid(), clone.GetFirstSenderUid());
        Assert(clone.Buffer.Data.Span.SequenceEqual(ev.Buffer.Data.Span));
    }

    private static void TestEventSerialization()
    {
        var source = new KEvent();
        source.Destination.PerformerId = 7;
        source.Destination.AddUid(10);
        source.Destination.AddUid(20);
        source.SetData(7, [100, 200], (ushort)SystemEventId.E_TOOL_SERVER_LIST_REQ, [1, 2, 3, 4]);

        var buffer = new KSerBuffer();
        var serializer = new KSerializer();
        serializer.BeginWriting(buffer);
        Assert(serializer.Put(source.Destination.PerformerId));
        Assert(serializer.Put(source.Destination.UidList.ToArray()));
        Assert(serializer.Put((ushort)source.EventId));
        Assert(serializer.Put(source.TraceToArray()));
        Assert(serializer.Put(source.Buffer.Data.ToArray()));
        serializer.EndWriting();

        buffer.Reset();
        serializer.BeginReading(buffer);
        Assert(serializer.Get(out uint performerId) && performerId == 7);
        Assert(serializer.Get(out long[] destinationUids));
        Assert(destinationUids.SequenceEqual([10, 20]));
        Assert(serializer.Get(out ushort eventId) && eventId == (ushort)source.EventId);
        Assert(serializer.Get(out long[] trace));
        Assert(trace.SequenceEqual([100, 200]));
        Assert(serializer.Get(out byte[] payload));
        Assert(payload.SequenceEqual([1, 2, 3, 4]));
        serializer.EndReading();
    }

    private static void TestEventFromTypeIsNotSerialized()
    {
        var buffer = new KSerBuffer();
        var serializer = new KSerializer();
        serializer.BeginWriting(buffer);
        Assert(!serializer.Put(new object()));
        serializer.EndWriting();
    }

    private static void Assert(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("Protocol compatibility assertion failed.");
    }

    private static void AssertEqual<T>(T expected, T actual) where T : IEquatable<T>
    {
        if (!expected.Equals(actual))
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}