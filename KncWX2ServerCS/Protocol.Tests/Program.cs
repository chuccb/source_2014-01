using KncWX2Server.Protocol;

static class Program
{
    static void Main()
    {
        TestLegacyLayout();
        TestExtendedLayout();
        TestTempBit();
        Console.WriteLine("KncUid compatibility vectors: PASS");
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

    private static void Assert(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("compatibility assertion failed");
    }

    private static void AssertEqual(long expected, long actual)
    {
        if (expected != actual)
            throw new InvalidOperationException($"expected 0x{expected:X16}, actual 0x{actual:X16}");
    }
}
