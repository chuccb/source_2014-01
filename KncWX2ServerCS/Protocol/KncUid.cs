namespace KncWX2Server.Protocol;

/// <summary>
/// 64-bit UID layout translated from X2ServerProtocol/KncUidType.h.
/// The original format reserves byte ranges for pure UID, server-group,
/// server and reserved identifiers.
/// </summary>
public static class KncUid
{
    public const long PureUidMask = 0x000000FFFFFFFFFFL;
    public const long ServerGroupMask = 0x3F00000000000000L;
    public const long ServerMask = 0x00FF000000000000L;
    public const long ReservedMask = 0x0000FF0000000000L;
    public const long TemporaryUidMask = 0x4000000000000000L;
    public const long SignBitMask = unchecked((long)0x8000000000000000UL);

    public static long ExtractPureUid(long uid) => uid & PureUidMask;
    public static long ExtractServerGroupId(long uid) => uid & ServerGroupMask;
    public static long ExtractServerId(long uid) => uid & ServerMask;
    public static long ExtractReservedId(long uid) => uid & ReservedMask;

    public static long SetPureUid(long destination, long source) =>
        (destination & ~PureUidMask) | (source & PureUidMask);

    public static long SetServerGroupId(long destination, long source) =>
        (destination & ~ServerGroupMask) | (source & ServerGroupMask);

    public static long SetServerId(long destination, long source) =>
        (destination & ~ServerMask) | (source & ServerMask);

    public static long SetReservedId(long destination, long source) =>
        (destination & ~ReservedMask) | (source & ReservedMask);

    public static bool IsTemporary(long uid) => (uid & TemporaryUidMask) != 0;
    public static bool IsServerGroup(long uid) => (uid & ServerGroupMask) != 0;
    public static bool IsServer(long uid) => (uid & ServerMask) != 0;
}
