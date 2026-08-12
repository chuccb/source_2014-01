using KncWX2Server.Protocol;

internal static class EventIdCompatibilityTests
{
    public static void Run()
    {
        VerifySystemValues();
        VerifyBoundary();
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
        AssertEqual(SystemEventId.E_SYSTEM_EVENT_ID_END, boundary.TryGetSystemId());
    }

    private static void VerifyUnknownRoundTrip()
    {
        const ushort value = 0xBEEF;
        var eventId = new EventId(value);

        Assert(!eventId.IsSystem);
        Assert(!eventId.IsSystemBoundary);
        AssertEqual(value, (ushort)eventId);
        AssertEqual(value, new EventId(value).Value);
        AssertEqual("UNKNOWN_EVENT_ID_48879", eventId.ToString());
    }

    private static void VerifyLegacyLookup()
    {
        var unknown = new EventId(ushort.MaxValue);
        AssertEqual(
            nameof(SystemEventId.E_SYSTEM_EVENT_ID_END),
            unknown.ToLegacyName());

        var ev = new KEvent();
        ev.SetData(0, [1, 2], unknown, ReadOnlySpan<byte>.Empty);
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
