namespace KncWX2Server.Protocol;

/// <summary>
/// Bit-exact counterpart of KncWX2Server/Common/KncUidType.h/.cpp.
///
/// The native implementation has two layouts selected by
/// EXTEND_SERVER_GROUP_MASK.  The legacy layout is the default used by the
/// original server unless SERV_COUNTRY_CN or SERV_COUNTRY_US enables the
/// extended layout.
/// </summary>
public static class KncUid
{
    public enum Layout
    {
        Legacy,
        ExtendedServerGroup,
    }

    public const long SignBitMask = unchecked((long)0x8000_0000_0000_0000UL);
    public const long TemporaryUidBit = unchecked((long)0x4000_0000_0000_0000UL);

    public static long Die32()
    {
        // Native Die32() returns a value in [1, UINT_MAX]. The exact PRNG
        // sequence is not part of the wire contract, so use a thread-safe
        // managed source while preserving the range and integer width.
        Span<byte> bytes = stackalloc byte[4];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes);
        return value == 0 ? 1 : value;
    }

    public static long GetTempUid(Layout layout = Layout.Legacy)
    {
        ulong pureMask = GetPureMask(layout);
        Span<byte> bytes = stackalloc byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        ulong value = BitConverter.ToUInt64(bytes) & pureMask;
        if (value == 0)
            value = 1;
        return unchecked((long)(value | (1UL << 62)));
    }

    public static long ExtractPureUid(long uid, Layout layout = Layout.Legacy) =>
        unchecked((long)(ToUInt64(uid) & GetPureMask(layout)));

    public static long ExtractServerGroupId(long uid, Layout layout = Layout.Legacy) =>
        unchecked((long)((ToUInt64(uid) >> GetServerGroupShift(layout)) & GetServerGroupValueMask(layout)));

    public static long ExtractServerId(long uid, Layout layout = Layout.Legacy) =>
        unchecked((long)((ToUInt64(uid) >> GetServerShift(layout)) & 0xFFUL));

    public static long ExtractReservedId(long uid, Layout layout = Layout.Legacy) =>
        unchecked((long)((ToUInt64(uid) >> GetReservedShift(layout)) & 0xFFUL));

    /// <summary>Combines the server-id and reserved-id fields.</summary>
    public static long ExtractCodeId(long uid, Layout layout = Layout.Legacy) =>
        unchecked((long)((ToUInt64(uid) >> GetReservedShift(layout)) & 0xFFFFUL));

    /// <summary>Sets bit 62, preserving every other bit exactly.</summary>
    public static long SetTempUid(long destination) =>
        unchecked((long)((ToUInt64(destination) & ~(1UL << 62)) | (1UL << 62)));

    public static long SetPureUid(long destination, long source, Layout layout = Layout.Legacy) =>
        unchecked((long)((ToUInt64(destination) & ~GetPureMask(layout)) |
                         (ToUInt64(source) & GetPureMask(layout))));

    public static long SetServerGroupId(long destination, long source, Layout layout = Layout.Legacy) =>
        SetField(destination, source, GetServerGroupShift(layout), GetServerGroupValueMask(layout));

    public static long SetServerId(long destination, long source, Layout layout = Layout.Legacy) =>
        SetField(destination, source, GetServerShift(layout), 0xFFUL);

    public static long SetReservedId(long destination, long source, Layout layout = Layout.Legacy) =>
        SetField(destination, source, GetReservedShift(layout), 0xFFUL);

    public static long SetCodeId(long destination, long source, Layout layout = Layout.Legacy) =>
        SetField(destination, source, GetReservedShift(layout), 0xFFFFUL);

    public static bool IsTemporary(long uid) => (ToUInt64(uid) & (1UL << 62)) != 0;

    private static long SetField(long destination, long source, int shift, ulong valueMask)
    {
        ulong fieldMask = valueMask << shift;
        return unchecked((long)((ToUInt64(destination) & ~fieldMask) |
                                ((ToUInt64(source) & valueMask) << shift)));
    }

    private static ulong ToUInt64(long value) => unchecked((ulong)value);

    private static ulong GetPureMask(Layout layout) => layout switch
    {
        Layout.Legacy => 0x0000_00FF_FFFF_FFFFUL,
        Layout.ExtendedServerGroup => 0x0000_000F_FFFF_FFFFUL,
        _ => throw new ArgumentOutOfRangeException(nameof(layout)),
    };

    private static int GetServerGroupShift(Layout layout) => layout switch
    {
        Layout.Legacy => 56,
        Layout.ExtendedServerGroup => 52,
        _ => throw new ArgumentOutOfRangeException(nameof(layout)),
    };

    private static ulong GetServerGroupValueMask(Layout layout) => layout switch
    {
        Layout.Legacy => 0x3FUL,
        Layout.ExtendedServerGroup => 0x3FFUL,
        _ => throw new ArgumentOutOfRangeException(nameof(layout)),
    };

    private static int GetServerShift(Layout layout) => layout switch
    {
        Layout.Legacy => 48,
        Layout.ExtendedServerGroup => 44,
        _ => throw new ArgumentOutOfRangeException(nameof(layout)),
    };

    private static int GetReservedShift(Layout layout) => layout switch
    {
        Layout.Legacy => 40,
        Layout.ExtendedServerGroup => 36,
        _ => throw new ArgumentOutOfRangeException(nameof(layout)),
    };
}
