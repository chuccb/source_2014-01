namespace KncWX2Server.Protocol;

/// <summary>Wire-level event identifier. Native KEvent stores this as an unsigned 16-bit value.</summary>
public readonly record struct EventId(ushort Value)
{
    public static implicit operator EventId(ushort value) => new(value);
    public static implicit operator ushort(EventId id) => id.Value;

    public bool IsSystem => Value < (ushort)SystemEventId.E_SYSTEM_EVENT_ID_END;
    public bool IsSystemBoundary => Value == (ushort)SystemEventId.E_SYSTEM_EVENT_ID_END;
    public bool IsServer => Value >= (ushort)SystemEventId.E_SYSTEM_EVENT_ID_END;

    public SystemEventId? TryGetSystemId()
        => IsSystem || IsSystemBoundary ? (SystemEventId)Value : null;

    public ServerEventId? TryGetServerId()
        => IsServer && Enum.IsDefined((ServerEventId)Value) ? (ServerEventId)Value : null;

    /// <summary>Managed full-event name. ID 20 is EVENT_X2_STARTUP; client IDs follow from 21.</summary>
    public override string ToString()
        => GeneratedEventIdNames.Get(Value)
           ?? EventIdNames.GetSystem(Value);

    /// <summary>Preserves native KEvent::GetIDStr behavior: IDs at or above the system boundary resolve to E_SYSTEM_EVENT_ID_END.</summary>
    public string ToLegacyName() => EventIdNames.GetLegacy(Value);
}

/// <summary>System event identifiers from the native EventID_System.h contract.</summary>
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

    /// <summary>System segment boundary. The server-event enum continues at this value.</summary>
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

    public static string GetLegacy(ushort value) =>
        value >= (ushort)SystemEventId.E_SYSTEM_EVENT_ID_END
            ? SystemNames[(ushort)SystemEventId.E_SYSTEM_EVENT_ID_END]
            : SystemNames[value];
}
