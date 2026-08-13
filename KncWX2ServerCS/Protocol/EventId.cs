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

    public static ushort SystemBoundary => GeneratedEventIdRanges.SystemBoundary;
    public static ushort ClientStart => GeneratedEventIdRanges.ClientStart;
    public static ushort ClientEnd => GeneratedEventIdRanges.ClientEnd;
    public static ushort ServerBoundary => GeneratedEventIdRanges.ServerBoundary;
    public static ushort ServerStart => GeneratedEventIdRanges.ServerStart;
    public static ushort ServerEnd => GeneratedEventIdRanges.ServerEnd;

    public bool IsSystem => Value < SystemBoundary;
    public bool IsSystemBoundary => Value == SystemBoundary;
    public bool IsClient => Value >= ClientStart && Value < ClientEnd;
    public bool IsClientBoundary => Value == ClientEnd;
    public bool IsServerBoundary => Value == ServerBoundary;
    public bool IsServer => Value >= ServerStart && Value < ServerEnd;
    public bool IsX2Server => Value >= SystemBoundary && Value <= ClientEnd;

    public SystemEventId? TryGetSystemId()
        => Value <= SystemBoundary
            ? (SystemEventId)Value
            : null;

    public ClientEventId? TryGetClientId()
        => IsClient || IsClientBoundary
            ? (ClientEventId)Value
            : null;

    public ServerEventId? TryGetServerId()
        => IsServerBoundary || IsServer
            ? (ServerEventId)Value
            : null;

    public X2ServerEventId? TryGetX2ServerId()
        => IsX2Server ? (X2ServerEventId)Value : null;

    public override string ToString()
        => GeneratedEventIdNames.Get(Value) ?? $"UNKNOWN_EVENT_ID_{Value}";

    /// <summary>Preserves native KEvent::GetIDStr: IDs at or above the system boundary resolve to the boundary entry.</summary>
    public string ToLegacyName()
        => Value >= SystemBoundary
            ? nameof(SystemEventId.E_SYSTEM_EVENT_ID_END)
            : ((SystemEventId)Value).ToString();
}
