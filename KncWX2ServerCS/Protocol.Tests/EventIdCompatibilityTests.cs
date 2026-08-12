using KncWX2Server.Protocol;

internal static class EventIdCompatibilityTests
{
    public static void Run()
    {
        VerifySystemValues();
        VerifyBoundary();
        VerifyFullClientSequence();
        VerifyUnknownRoundTrip();
        VerifyPerformerLimit();
        VerifyLegacyLookup();
    }

    private static void VerifySystemValues()
    {
        var expected = Enum.GetValues<SystemEventId>();
        for (var index = 0; index < expected.Length; index++)
        {
            var id = expected[index];
            var eventId = new EventId((ushort)id);

            AssertEqual(index, (ushort)id);
            AssertEqual(id, eventId.TryGetSystemId());
            AssertEqual(id.ToString(), eventId.ToString());
        }

        AssertEqual((ushort)20, (ushort)SystemEventId.E_SYSTEM_EVENT_ID_END);
    }

    private static void VerifyBoundary()
    {
        var boundary = new EventId((ushort)SystemEventId.E_SYSTEM_EVENT_ID_END);

        Assert(!boundary.IsSystem);
        Assert(boundary.IsSystemBoundary);
        Assert(boundary.IsServer);
        AssertEqual(SystemEventId.E_SYSTEM_EVENT_ID_END, boundary.TryGetSystemId());
        AssertEqual(ServerEventId.EVENT_X2_STARTUP, boundary.TryGetServerId());
        AssertEqual(nameof(ServerEventId.EVENT_X2_STARTUP), boundary.ToString());
    }

    private static void VerifyFullClientSequence()
    {
        AssertEqual((ushort)21, (ushort)ServerEventId.EGS_GOOD_JOB_1_REQ);
        AssertEqual((ushort)22, (ushort)ServerEventId.EGS_GOOD_JOB_2_REQ);
        AssertEqual((ushort)1222, (ushort)ServerEventId.EGS_READY_TO_SOSUN_EVENT_ACK);
        AssertEqual((ushort)1223, (ushort)ServerEventId.EGS_CLIENT_EVENT_ID_END);

        AssertEqual(
            nameof(ServerEventId.EGS_GOOD_JOB_1_REQ),
            new EventId(21).ToString());
        AssertEqual(
            nameof(ServerEventId.EGS_READY_TO_SOSUN_EVENT_ACK),
            new EventId(1222).ToString());
        AssertEqual(
            nameof(ServerEventId.EGS_CLIENT_EVENT_ID_END),
            new EventId(1223).ToString());

        for (ushort value = 21; value < 1223; value++)
        {
            var id = new EventId(value);
            Assert(id.IsServer);
            Assert(id.TryGetServerId() is not null);
            Assert(!string.IsNullOrEmpty(id.ToString()));
        }
    }

    private static void VerifyUnknownRoundTrip()
    {
        const ushort value = 0xBEEF;
        var eventId = new EventId(value);

        Assert(!eventId.IsSystem);
        Assert(!eventId.IsSystemBoundary);
        Assert(eventId.IsServer);
        AssertEqual(value, (ushort)eventId);
        AssertEqual(value, new EventId(value).Value);
        AssertEqual("UNKNOWN_EVENT_ID_48879", eventId.ToString());
        Assert(eventId.TryGetServerId() is null);
    }

    private static void VerifyLegacyLookup()
    {
        var ids = new ushort[] { 20, 21, 1223, ushort.MaxValue };
        foreach (var id in ids)
        {
            AssertEqual(
                nameof(SystemEventId.E_SYSTEM_EVENT_ID_END),
                new EventId(id).ToLegacyName());
        }

        var ev = new KEvent();
        ev.SetData(0, [1, 2], ushort.MaxValue, ReadOnlySpan<byte>.Empty);
        AssertEqual(nameof(SystemEventId.E_SYSTEM_EVENT_ID_END), ev.GetIdString());
    }

    private static void VerifyPerformerLimit()
    {
        var performer = new KPerformerInfo();
        for (var uid = 0; uid < KPerformerInfo.MaxUidNum; uid++)
            Assert(performer.AddUid(uid));

        Assert(!performer.AddUid(KPerformerInfo.MaxUidNum));
        AssertEqual(KPerformerInfo.MaxUidNum, performer.UidListSize);
    }

    private static void Assert(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("EventId compatibility assertion failed.");
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}
