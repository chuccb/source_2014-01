namespace KncWX2Server.Protocol;

public enum SystemEventId : ushort
{
    E_HEART_BEAT = 0,
    E_ACCEPT_CONNECTION_NOT,
    E_CONNECTION_LOST_NOT,
    E_UDP_PORT_NOT,
    E_DISABLE_HB_CHECK_REQ,
    E_LOG_NOT,
    E_RESERVE_DESTROY,
    E_TOOL_GET_CCU_INFO_REQ,
    E_TOOL_GET_CCU_INFO_ACK,
    E_TOOL_CHECK_LOGIN_REQ,
    E_TOOL_CHECK_LOGIN_ACK,
    E_TOOL_SERVER_LIST_REQ,
    E_TOOL_SERVER_LIST_ACK,
    E_CHECK_SEQUENCE_COUNT_NOT,
    E_UDP_RELAY_SERVER_CHECK_PACKET_NOT,
    E_CONNECT_RELAY_ACK,
    E_CHECK_DDOS_GUARD_REQ,
    E_CHECK_DDOS_GUARD_ACK,
    E_CH_CONNECTION_LOST_FOR_DDOS_GUARD_NOT,
    E_GS_CONNECTION_LOST_FOR_DDOS_GUARD_NOT,
    E_SYSTEM_EVENT_ID_END,
}

public sealed class KPerformerInfo
{
    public const int MaxUidNum = 2000;
    private readonly SortedSet<long> _uids = [];
    public uint PerformerId { get; set; }
    public int UidListSize => _uids.Count;
    public IReadOnlyCollection<long> UidList => _uids;
    public bool FindUid(long uid) => _uids.Contains(uid);
    public bool AddUid(long uid) { if (_uids.Count >= MaxUidNum) return false; _uids.Add(uid); return true; }
    internal void ClearUids() => _uids.Clear();
    public long GetFirstUid() => _uids.Count == 0 ? -1 : _uids.Min;
    public KPerformerInfo Clone() { var clone = new KPerformerInfo { PerformerId = PerformerId }; clone._uids.UnionWith(_uids); return clone; }
}

/// <summary>Managed counterpart of KncWX2Server/Common/Event.cpp.</summary>
public sealed class KEvent
{
    public enum EventFromType : ushort { None = 0, Server = 1, Client = 2 }

    private static readonly string[] SystemEventNames =
    [
        nameof(SystemEventId.E_HEART_BEAT), nameof(SystemEventId.E_ACCEPT_CONNECTION_NOT), nameof(SystemEventId.E_CONNECTION_LOST_NOT),
        nameof(SystemEventId.E_UDP_PORT_NOT), nameof(SystemEventId.E_DISABLE_HB_CHECK_REQ), nameof(SystemEventId.E_LOG_NOT),
        nameof(SystemEventId.E_RESERVE_DESTROY), nameof(SystemEventId.E_TOOL_GET_CCU_INFO_REQ), nameof(SystemEventId.E_TOOL_GET_CCU_INFO_ACK),
        nameof(SystemEventId.E_TOOL_CHECK_LOGIN_REQ), nameof(SystemEventId.E_TOOL_CHECK_LOGIN_ACK), nameof(SystemEventId.E_TOOL_SERVER_LIST_REQ),
        nameof(SystemEventId.E_TOOL_SERVER_LIST_ACK), nameof(SystemEventId.E_CHECK_SEQUENCE_COUNT_NOT), nameof(SystemEventId.E_UDP_RELAY_SERVER_CHECK_PACKET_NOT),
        nameof(SystemEventId.E_CONNECT_RELAY_ACK), nameof(SystemEventId.E_CHECK_DDOS_GUARD_REQ), nameof(SystemEventId.E_CHECK_DDOS_GUARD_ACK),
        nameof(SystemEventId.E_CH_CONNECTION_LOST_FOR_DDOS_GUARD_NOT), nameof(SystemEventId.E_GS_CONNECTION_LOST_FOR_DDOS_GUARD_NOT),
        nameof(SystemEventId.E_SYSTEM_EVENT_ID_END),
    ];

    public KPerformerInfo Destination { get; } = new();
    public long FirstTrace { get; private set; } = -1;
    public long LastTrace { get; private set; } = -1;
    public ushort EventId { get; private set; }
    public EventFromType FromType { get; private set; }
    public KSerBuffer Buffer { get; } = new();

    public long GetFirstSenderUid() => FirstTrace;
    public long GetLastSenderUid() => LastTrace == -1 ? FirstTrace : LastTrace;
    public bool IsEmptyTrace => FirstTrace == -1;

    public void SetData(uint performerId, ReadOnlySpan<long> trace, ushort eventId)
    {
        Destination.PerformerId = performerId;
        EventId = eventId;
        if (trace.IsEmpty) { FirstTrace = -1; LastTrace = -1; return; }
        if (trace.Length < 2) throw new ArgumentException("Native KEvent requires two trace slots when the trace pointer is non-null.", nameof(trace));
        FirstTrace = trace[0];
        LastTrace = trace[1];
    }

    /// <summary>Equivalent to the native templated SetData overload.</summary>
    public void SetData<T>(uint performerId, ReadOnlySpan<long> trace, ushort eventId, T data, Func<KSerializer, T, bool> put)
    {
        ArgumentNullException.ThrowIfNull(put);
        SetData(performerId, trace, eventId);
        Buffer.Clear();
        var serializer = new KSerializer();
        serializer.BeginWriting(Buffer);
        try
        {
            if (!put(serializer, data)) throw new InvalidOperationException("Native serializer returned false while constructing KEvent data.");
        }
        finally { serializer.EndWriting(); }
    }

    public void SetData(uint performerId, ReadOnlySpan<long> trace, ushort eventId, ReadOnlySpan<byte> payload)
    {
        SetData(performerId, trace, eventId);
        Buffer.Clear();
        if (!payload.IsEmpty) Buffer.Write(payload);
    }

    public void PushTrace(long uid) { if (FirstTrace == -1) FirstTrace = uid; else LastTrace = uid; }
    public void PopTrace() { if (LastTrace != -1) LastTrace = -1; else FirstTrace = -1; }

    public string GetIdString() => GetIdString(EventId);
    public static string GetIdString(ushort eventId) => eventId >= SystemEventNames.Length - 1 ? SystemEventNames[^1] : SystemEventNames[eventId];
    public void SetFromType(EventFromType type) => FromType = type;

    /// <summary>The complete server-event ID set is supplied by generated EventID_Server bindings.</summary>
    public bool IsValidEventId(IReadOnlySet<ushort>? serverEventIds = null)
        => FromType != EventFromType.Client || serverEventIds is null || !serverEventIds.Contains(EventId);

    public KEvent Clone()
    {
        var clone = new KEvent { EventId = EventId, FirstTrace = FirstTrace, LastTrace = LastTrace, FromType = FromType };
        clone.Destination.PerformerId = Destination.PerformerId;
        foreach (long uid in Destination.UidList) clone.Destination.AddUid(uid);
        if (Buffer.Length != 0) clone.Buffer.Write(Buffer.Data.Span);
        if (Buffer.IsCompressed) clone.Buffer.MarkCompressed();
        return clone;
    }
}
