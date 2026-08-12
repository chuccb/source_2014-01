namespace KncWX2Server.Protocol;

/// <summary>Wire-level event identifier stored by native KEvent as unsigned short.</summary>
public readonly record struct EventId(ushort Value)
{
    public static implicit operator EventId(ushort value) => new(value);
    public static implicit operator EventId(SystemEventId value) => new((ushort)value);
    public static implicit operator EventId(ClientEventId value) => new((ushort)value);
    public static implicit operator EventId(ServerEventId value) => new((ushort)value);
    public static implicit operator EventId(X2ServerEventId value) => new((ushort)value);
    public static implicit operator ushort(EventId id) => id.Value;

    public bool IsSystem => Value < (ushort)SystemEventId.E_SYSTEM_EVENT_ID_END;
    public bool IsSystemBoundary => Value == (ushort)SystemEventId.E_SYSTEM_EVENT_ID_END;
    public bool IsClient => Value >= GeneratedEventIdRanges.ClientStart && Value < GeneratedEventIdRanges.ClientEnd;
    public bool IsClientBoundary => Value == GeneratedEventIdRanges.ClientEnd;
    public bool IsServer => Value >= GeneratedEventIdRanges.ServerStart && Value < GeneratedEventIdRanges.ServerEnd;
    public bool IsServerBoundary => Value == GeneratedEventIdRanges.ServerEnd;
    public bool IsX2Server => Value >= (ushort)SystemEventId.E_SYSTEM_EVENT_ID_END && Value < GeneratedEventIdRanges.ClientEnd;

    public SystemEventId? TryGetSystemId()
        => Value <= (ushort)SystemEventId.E_SYSTEM_EVENT_ID_END
            ? (SystemEventId)Value
            : null;

    public ClientEventId? TryGetClientId()
        => IsClient ? (ClientEventId)Value : null;

    public ServerEventId? TryGetServerId()
        => IsServer || IsServerBoundary ? (ServerEventId)Value : null;

    public X2ServerEventId? TryGetX2ServerId()
        => IsX2Server ? (X2ServerEventId)Value : null;

    public override string ToString()
        => GeneratedEventIdNames.Get(Value)
           ?? EventIdNames.GetSystem(Value);

    /// <summary>Preserves native KEvent::GetIDStr: IDs at or above the system boundary resolve to the boundary entry.</summary>
    public string ToLegacyName() => EventIdNames.GetLegacy(Value);
}

/// <summary>System event identifiers from native EventID_System.h.</summary>
public enum SystemEventId : ushort
{
    E_HEART_BEAT = 0,
    E_ACCEPT_CONNECTION_NOT = 1,
    E_CONNECTION_LOST_NOT = 2,
    E_UDP_PORT_NOT = 3,
    E_DISABLE_HB_CHECK_REQ = 4,
    E_LOG_NOT = 5,
    E_RESERVE_DESTROY = 6,
    E_TOOL_GET_CCU_INFO_REQ = 7,
    E_TOOL_GET_CCU_INFO_ACK = 8,
    E_TOOL_CHECK_LOGIN_REQ = 9,
    E_TOOL_CHECK_LOGIN_ACK = 10,
    E_TOOL_SERVER_LIST_REQ = 11,
    E_TOOL_SERVER_LIST_ACK = 12,
    E_CHECK_SEQUENCE_COUNT_NOT = 13,
    E_UDP_RELAY_SERVER_CHECK_PACKET_NOT = 14,
    E_CONNECT_RELAY_ACK = 15,
    E_CHECK_DDOS_GUARD_REQ = 16,
    E_CHECK_DDOS_GUARD_ACK = 17,
    E_CH_CONNECTION_LOST_FOR_DDOS_GUARD_NOT = 18,
    E_GS_CONNECTION_LOST_FOR_DDOS_GUARD_NOT = 19,
    E_SYSTEM_EVENT_ID_END = 20,
}

internal static class EventIdNames
{
    private static readonly string[] SystemNames =
    [
        nameof(SystemEventId.E_HEART_BEAT),
        nameof(SystemEventId.E_ACCEPT_CONNECTION_NOT),
        nameof(SystemEventId.E_CONNECTION_LOST_NOT),
        nameof(SystemEventId.E_UDP_PORT_NOT),
        nameof(SystemEventId.E_DISABLE_HB_CHECK_REQ),
        nameof(SystemEventId.E_LOG_NOT),
        nameof(SystemEventId.E_RESERVE_DESTROY),
        nameof(SystemEventId.E_TOOL_GET_CCU_INFO_REQ),
        nameof(SystemEventId.E_TOOL_GET_CCU_INFO_ACK),
        nameof(SystemEventId.E_TOOL_CHECK_LOGIN_REQ),
        nameof(SystemEventId.E_TOOL_CHECK_LOGIN_ACK),
        nameof(SystemEventId.E_TOOL_SERVER_LIST_REQ),
        nameof(SystemEventId.E_TOOL_SERVER_LIST_ACK),
        nameof(SystemEventId.E_CHECK_SEQUENCE_COUNT_NOT),
        nameof(SystemEventId.E_UDP_RELAY_SERVER_CHECK_PACKET_NOT),
        nameof(SystemEventId.E_CONNECT_RELAY_ACK),
        nameof(SystemEventId.E_CHECK_DDOS_GUARD_REQ),
        nameof(SystemEventId.E_CHECK_DDOS_GUARD_ACK),
        nameof(SystemEventId.E_CH_CONNECTION_LOST_FOR_DDOS_GUARD_NOT),
        nameof(SystemEventId.E_GS_CONNECTION_LOST_FOR_DDOS_GUARD_NOT),
        nameof(SystemEventId.E_SYSTEM_EVENT_ID_END),
    ];

    public static string GetSystem(ushort value)
        => value < SystemNames.Length ? SystemNames[value] : $"UNKNOWN_EVENT_ID_{value}";

    public static string GetLegacy(ushort value)
        => value >= (ushort)SystemEventId.E_SYSTEM_EVENT_ID_END
            ? SystemNames[(ushort)SystemEventId.E_SYSTEM_EVENT_ID_END]
            : SystemNames[value];
}
