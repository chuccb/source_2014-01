using KncWX2Server.Protocol;

internal static class EventWireCompatibilityTests
{
    public static void Run()
    {
        RoundTripSystemEvent();
        RoundTripClientEvent();
        RoundTripServerEvent();
        PreserveTraceAndPerformerSet();
        PreservePayloadBytes();
    }

    private static void RoundTripSystemEvent()
    {
        var source = CreateEvent(SystemEventId.E_TOOL_SERVER_LIST_REQ, [10, 20], [1, 2, 3, 4]);
        var restored = RoundTrip(source);

        AssertEqual(source.Id, restored.Id);
        AssertEqual(source.GetFirstSenderUid(), restored.GetFirstSenderUid());
        AssertEqual(source.GetLastSenderUid(), restored.GetLastSenderUid());
        AssertEqual(source.Destination.PerformerId, restored.Destination.PerformerId);
        AssertSequenceEqual(source.Destination.UidList, restored.Destination.UidList);
        Assert(source.Buffer.Data.Span.SequenceEqual(restored.Buffer.Data.Span));
        Assert(!restored.Buffer.IsCompressed);
    }

    private static void RoundTripClientEvent()
    {
        var source = CreateEvent(ClientEventId.EGS_GOOD_JOB_1_REQ, [100, 200], [0x10, 0x20]);
        var restored = RoundTrip(source);

        AssertEqual(source.Id, restored.Id);
        AssertEqual(nameof(ClientEventId.EGS_GOOD_JOB_1_REQ), restored.Id.ToString());
        AssertEqual(nameof(SystemEventId.E_SYSTEM_EVENT_ID_END), restored.GetIdString());
    }

    private static void RoundTripServerEvent()
    {
        var source = CreateEvent(GetFirstServerEventId(), [300, 400], [0x30, 0x40]);
        var restored = RoundTrip(source);

        AssertEqual(source.Id, restored.Id);
        Assert(restored.Id.IsServer);
        AssertEqual(source.Id.ToString(), restored.Id.ToString());
    }

    private static void PreserveTraceAndPerformerSet()
    {
        var source = CreateEvent(ClientEventId.EGS_GOOD_JOB_2_REQ, [-1, 0x1234], [0xAA]);
        source.Destination.AddUid(30);
        source.Destination.AddUid(10);
        source.Destination.AddUid(20);

        var restored = RoundTrip(source);

        AssertEqual(-1L, restored.GetFirstSenderUid());
        AssertEqual(0x1234L, restored.GetLastSenderUid());
        AssertEqual(10L, restored.Destination.GetFirstUid());
        AssertSequenceEqual([10L, 20L, 30L], restored.Destination.UidList);
    }

    private static void PreservePayloadBytes()
    {
        var source = CreateEvent(SystemEventId.E_HEART_BEAT, [1, 2], [0x01, 0x02]);
        var buffer = new KSerBuffer();
        var serializer = new KSerializer();

        serializer.BeginWriting(buffer, tagging: true);
        Assert(serializer.PutEvent(source));
        serializer.EndWriting();

        var bytes = buffer.Data.ToArray();
        bytes[^1] ^= 0xFF;

        var mutated = new KSerBuffer();
        mutated.Write(bytes);
        var restored = new KEvent();
        serializer.BeginReading(mutated, tagging: true);
        Assert(serializer.GetEvent(restored));
        serializer.EndReading();

        Assert(!restored.Buffer.Data.Span.SequenceEqual(source.Buffer.Data.Span));
    }

    private static KEvent CreateEvent(EventId eventId, long[] trace, byte[] payload)
    {
        var value = new KEvent();
        value.SetData(77, trace, eventId, payload);
        value.Destination.AddUid(20);
        value.Destination.AddUid(10);
        return value;
    }

    private static ServerEventId GetFirstServerEventId()
        => (ServerEventId)GeneratedServerEventRange.Start;

    private static KEvent RoundTrip(KEvent source)
    {
        var buffer = new KSerBuffer();
        var serializer = new KSerializer();

        serializer.BeginWriting(buffer, tagging: true);
        Assert(serializer.PutEvent(source));
        serializer.EndWriting();

        buffer.Reset();
        var restored = new KEvent();
        serializer.BeginReading(buffer, tagging: true);
        Assert(serializer.GetEvent(restored));
        serializer.EndReading();
        return restored;
    }

    private static void Assert(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("KEvent wire compatibility assertion failed.");
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }

    private static void AssertSequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        if (!expected.SequenceEqual(actual))
            throw new InvalidOperationException("Expected sequences to be equal.");
    }
}

internal static class GeneratedServerEventRange
{
    public const ushort Start = 1223;
}
