namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// System event identifiers declared by EventID_System.h. The native header's
/// sentinel is part of the enum and is required as the beginning of the client
/// event range, so it is retained explicitly here.
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
    E_SYSTEM_EVENT_ID_END,
}

/// <summary>
/// Exact managed representation of native KPerformerInfo.
/// Native storage is std::set&lt;UidType&gt;, and UidType is __int64, so long plus
/// SortedSet&lt;long&gt; preserves the native value width, uniqueness and traversal order.
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

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) => value.WriteTo(ser));

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KPerformerInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(
            out value,
            static (ser, existing) => TryReadFrom(ser, existing)
                ? (true, existing)
                : (false, existing));
    }

    internal bool WriteTo(NativePrimitiveSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        serializer.Put(PerformerId);
        new NativeStlSerializer(serializer).PutSet(_uids, static (ser, uid) => ser.Put(uid));
        return true;
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

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) => value.WriteTo(ser));

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEvent value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(
            out value,
            static (ser, existing) => TryReadFrom(ser, existing)
                ? (true, existing)
                : (false, existing));
    }

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

    internal bool WriteTo(NativePrimitiveSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        // Native ks.Put(m_kDestPerformer) dispatches through the USERCLASS
        // overload, so the nested eTAG_USERCLASS is emitted when tagging is enabled.
        if (!Destination.Serialize(serializer))
            return false;

        serializer.Put(FirstTrace);
        serializer.Put(LastTrace);
        serializer.Put(EventId);
        new NativeBufferSerializer(serializer).Put(Buffer);
        return true;
    }

    internal static bool TryReadFrom(NativePrimitiveSerializer serializer, KEvent value)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(value);

        // Match native ks.Get(m_kDestPerformer): consume the nested USERCLASS tag
        // before reading KPerformerInfo's fields.
        if (!KPerformerInfo.TryDeserialize(serializer, out var destination) ||
            !serializer.TryGet(out long firstTrace) ||
            !serializer.TryGet(out long lastTrace) ||
            !serializer.TryGet(out ushort eventId) ||
            !new NativeBufferSerializer(serializer).TryGet(value.Buffer))
            return false;

        value.Destination.PerformerId = destination.PerformerId;
        foreach (var uid in destination.UidList)
            value.Destination.AddUid(uid);
        value.FirstTrace = firstTrace;
        value.LastTrace = lastTrace;
        value.EventId = eventId;
        return true;
    }
}
