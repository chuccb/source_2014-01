using KncWX2Server.Protocol;

internal static class EventIdCompatibilityTests
{
    public static void Run()
    {
        VerifySystemValues();
        VerifyX2Boundary();
        VerifyClientRange();
        VerifyServerRange();
        VerifyUnknownRoundTrip();
        VerifyPerformerLimit();
        VerifyLegacyLookup();
    }

    private static void VerifySystemValues()
    {
        var expected = Enum.GetValues<SystemEventId>()
            .Where(id => id != SystemEventId.E_SYSTEM_EVENT_ID_END)
            .ToArray();

        for (var index = 0; index < expected.Length; index++)
        {
            var id = expected[index];
            var eventId = new EventId(id);

            AssertEqual((ushort)index, (ushort)id);
            AssertEqual(id, eventId.TryGetSystemId());
            AssertEqual(id.ToString(), eventId.ToString());
        }

        AssertEqual((ushort)20, (ushort)SystemEventId.E_SYSTEM_EVENT_ID_END);
    }

    private static void VerifyX2Boundary()
    {
        var startup = new EventId(SystemEventId.E_SYSTEM_EVENT_ID_END);

        Assert(!startup.IsSystem);
        Assert(startup.IsSystemBoundary);
        Assert(startup.IsX2Server);
        Assert(!startup.IsServer);
        AssertEqual(SystemEventId.E_SYSTEM_EVENT_ID_END, startup.TryGetSystemId());
        AssertEqual(X2ServerEventId.EVENT_X2_STARTUP, startup.TryGetX2ServerId());
        AssertEqual(nameof(X2ServerEventId.EVENT_X2_STARTUP), startup.ToString());
    }

    private static void VerifyClientRange()
    {
        AssertEqual((ushort)21, (ushort)ClientEventId.EGS_GOOD_JOB_1_REQ);
        AssertEqual((ushort)22, (ushort)ClientEventId.EGS_GOOD_JOB_2_REQ);
        AssertEqual((ushort)1222, (ushort)ClientEventId.EGS_READY_TO_SOSUN_EVENT_ACK);
        AssertEqual((ushort)1223, (ushort)ClientEventId.EGS_CLIENT_EVENT_ID_END);
        AssertEqual((ushort)21, EventId.ClientStart);
        AssertEqual((ushort)1223, EventId.ClientEnd);

        for (ushort value = EventId.ClientStart; value < EventId.ClientEnd; value++)
        {
            var id = new EventId(value);
            Assert(id.IsClient);
            AssertEqual((ClientEventId)value, id.TryGetClientId());
            Assert(!string.IsNullOrEmpty(id.ToString()));
        }

        var end = new EventId(EventId.ClientEnd);
        Assert(end.IsClientBoundary);
        Assert(!end.IsClient);
        AssertEqual(ClientEventId.EGS_CLIENT_EVENT_ID_END, end.TryGetClientId());
    }

    private static void VerifyServerRange()
    {
        AssertEqual((ushort)1223, EventId.ServerBoundary);
        AssertEqual((ushort)1224, EventId.ServerStart);
        Assert(EventId.ServerEnd > EventId.ServerStart);

        var first = new EventId(EventId.ServerStart);
        Assert(first.IsServer);
        AssertEqual((ServerEventId)EventId.ServerStart, first.TryGetServerId());
        Assert(!string.IsNullOrEmpty(first.ToString()));

        var boundary = new EventId(EventId.ServerBoundary);
        Assert(boundary.IsServerBoundary);
        Assert(!boundary.IsServer);
        AssertEqual(ServerEventId.E_SERVER_EVENT_ID_BEGIN, boundary.TryGetServerId());
    }

    private static void VerifyUnknownRoundTrip()
    {
        const ushort value = 0xBEEF;
        var eventId = new EventId(value);

        Assert(!eventId.IsSystem);
        Assert(!eventId.IsSystemBoundary);
        Assert(!eventId.IsClient);
        Assert(!eventId.IsServer);
        Assert(!eventId.IsX2Server);
        AssertEqual(value, (ushort)eventId);
        AssertEqual(value, new EventId(value).Value);
        AssertEqual("UNKNOWN_EVENT_ID_48879", eventId.ToString());
        Assert(eventId.TryGetServerId() is null);
        Assert(eventId.TryGetClientId() is null);
    }

    private static void VerifyLegacyLookup()
    {
        foreach (var id in new ushort[] { 20, 21, EventId.ClientEnd, EventId.ServerStart, ushort.MaxValue })
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
