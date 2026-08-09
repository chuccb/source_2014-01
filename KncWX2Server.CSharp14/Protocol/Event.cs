namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// System event identifiers declared by EventID_System.h before the optional
/// global-event include. The global/client/server event lists are kept separate
/// because their numeric ranges are assembled by the native preprocessor.
/// </summary>
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
}

/// <summary>
/// Exact managed representation of native KPerformerInfo.
/// Native storage is std::set&lt;UidType&gt;, so SortedSet&lt;long&gt; preserves both
/// uniqueness and the traversal order used by serialization.
/// </summary>
public sealed class KPerformerInfo
{
    public const int MaxUidNum = 2000;

    private readonly SortedSet<long> _uids = [];

    public uint PerformerId { get; set; }
    public int UidListSize => _uids.Count;
    public IReadOnlyCollection<long> UidList => _uids;

    public bool FindUid(long uid) => _uids.Contains(uid);

    public bool AddUid(long uid)
    {
        if (_uids.Count >= MaxUidNum)
            return false;

        _uids.Add(uid);
        return true;
    }

    public long GetFirstUid() => _uids.Count == 0 ? -1 : _uids.Min;

    public KPerformerInfo Clone()
    {
        var clone = new KPerformerInfo { PerformerId = PerformerId };
        clone._uids.UnionWith(_uids);
        return clone;
    }

    internal void WriteTo(NativePrimitiveSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        serializer.Put(PerformerId);
        new NativeStlSerializer(serializer).PutSet(_uids, static (ser, uid) => ser.Put(uid));
    }

    internal static bool TryReadFrom(NativePrimitiveSerializer serializer, KPerformerInfo value)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(value);

        if (!serializer.TryGet(out uint performerId))
            return false;

        var stl = new NativeStlSerializer(serializer);
        if (!stl.TryGetSet(out SortedSet<long> uids, static ser =>
            ser.TryGet(out long uid) ? (true, uid) : (false, default)))
            return false;

        value.PerformerId = performerId;
        value._uids.Clear();
        value._uids.UnionWith(uids);
        return true;
    }
}

/// <summary>
/// Managed counterpart of native KEvent.
/// FromType is runtime metadata and is intentionally not serialized: native
/// SERIALIZE_DEFINE_PUT/GET serializes destination, the two trace UIDs, event ID,
/// and KSerBuffer only.
/// </summary>
public sealed class KEvent
{
    public enum EventFromType : ushort
    {
        None = 0,
        Server = 1,
        Client = 2,
    }

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

        if (trace.Length >= 2)
        {
            FirstTrace = trace[0];
            LastTrace = trace[1];
        }
        else
        {
            FirstTrace = -1;
            LastTrace = -1;
        }
    }

    public void SetData(uint performerId, ReadOnlySpan<long> trace, ushort eventId, ReadOnlySpan<byte> payload)
    {
        SetData(performerId, trace, eventId);
        Buffer.SetData(payload);
    }

    public void PushTrace(long uid)
    {
        if (FirstTrace == -1)
            FirstTrace = uid;
        else
            LastTrace = uid;
    }

    public void PopTrace()
    {
        if (LastTrace != -1)
            LastTrace = -1;
        else
            FirstTrace = -1;
    }

    public void SetFromType(EventFromType type) => FromType = type;

    public KEvent Clone()
    {
        var clone = new KEvent
        {
            EventId = EventId,
            FirstTrace = FirstTrace,
            LastTrace = LastTrace,
            FromType = FromType,
        };

        clone.Destination.PerformerId = Destination.PerformerId;
        foreach (var uid in Destination.UidList)
            clone.Destination.AddUid(uid);

        clone.Buffer.SetData(Buffer.WrittenMemory.Span, Buffer.IsCompressed);
        return clone;
    }

    internal void WriteTo(NativePrimitiveSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        Destination.WriteTo(serializer);
        serializer.Put(FirstTrace);
        serializer.Put(LastTrace);
        serializer.Put(EventId);
        new NativeBufferSerializer(serializer).Put(Buffer);
    }

    internal static bool TryReadFrom(NativePrimitiveSerializer serializer, KEvent value)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(value);

        if (!KPerformerInfo.TryReadFrom(serializer, value.Destination) ||
            !serializer.TryGet(out long firstTrace) ||
            !serializer.TryGet(out long lastTrace) ||
            !serializer.TryGet(out ushort eventId) ||
            !new NativeBufferSerializer(serializer).TryGet(value.Buffer))
            return false;

        value.FirstTrace = firstTrace;
        value.LastTrace = lastTrace;
        value.EventId = eventId;
        return true;
    }
}
