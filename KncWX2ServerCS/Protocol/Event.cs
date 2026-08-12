namespace KncWX2Server.Protocol;

public sealed class KPerformerInfo
{
    public const int MaxUidNum = 100;

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

    internal void ClearUids() => _uids.Clear();

    public long GetFirstUid() => _uids.Count == 0 ? -1 : _uids.Min;

    public KPerformerInfo Clone()
    {
        var clone = new KPerformerInfo { PerformerId = PerformerId };
        clone._uids.UnionWith(_uids);
        return clone;
    }
}

/// <summary>Managed counterpart of the native X2ServerProtocol KEvent.</summary>
public sealed class KEvent
{
    public enum EventFromType : ushort
    {
        None,
        Server,
        Client,
    }

    public KPerformerInfo Destination { get; } = new();
    public long FirstTrace { get; private set; } = -1;
    public long LastTrace { get; private set; } = -1;
    public EventId Id { get; private set; }
    public EventFromType FromType { get; private set; }
    public KSerBuffer Buffer { get; } = new();

    /// <summary>Compatibility property for existing call sites that use the native ushort field name.</summary>
    public ushort EventId
    {
        get => Id.Value;
        private set => Id = value;
    }

    public long GetFirstSenderUid() => FirstTrace;
    public long GetLastSenderUid() => LastTrace == -1 ? FirstTrace : LastTrace;
    public bool IsEmptyTrace => FirstTrace == -1;

    public void SetData(uint performerId, ReadOnlySpan<long> trace, EventId eventId)
    {
        Destination.PerformerId = performerId;
        Id = eventId;

        if (trace.IsEmpty)
        {
            FirstTrace = -1;
            LastTrace = -1;
            return;
        }

        if (trace.Length < 2)
            throw new ArgumentException("A non-empty native event trace contains two slots.", nameof(trace));

        FirstTrace = trace[0];
        LastTrace = trace[1];
    }

    public void SetData<T>(
        uint performerId,
        ReadOnlySpan<long> trace,
        EventId eventId,
        T data,
        Func<KSerializer, T, bool> put)
    {
        ArgumentNullException.ThrowIfNull(put);
        SetData(performerId, trace, eventId);
        Buffer.Clear();

        var serializer = new KSerializer();
        serializer.BeginWriting(Buffer);
        try
        {
            if (!put(serializer, data))
                throw new InvalidOperationException("Serializer rejected KEvent payload.");
        }
        finally
        {
            serializer.EndWriting();
        }
    }

    public void SetData(
        uint performerId,
        ReadOnlySpan<long> trace,
        EventId eventId,
        ReadOnlySpan<byte> payload)
    {
        SetData(performerId, trace, eventId);
        Buffer.Clear();

        if (!payload.IsEmpty)
            Buffer.Write(payload);
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

    public string GetIdString() => Id.ToString();
    public static string GetIdString(EventId eventId) => eventId.ToString();
    public void SetFromType(EventFromType type) => FromType = type;

    /// <summary>Preserves the legacy server-event validation hook until the missing native client-event table is restored.</summary>
    public bool IsValidEventId(IReadOnlySet<ushort>? serverEventIds = null)
        => FromType != EventFromType.Client || serverEventIds is null || !serverEventIds.Contains(Id.Value);

    public KEvent Clone()
    {
        var clone = new KEvent
        {
            Id = Id,
            FirstTrace = FirstTrace,
            LastTrace = LastTrace,
            FromType = FromType,
        };

        clone.Destination.PerformerId = Destination.PerformerId;
        foreach (var uid in Destination.UidList)
            clone.Destination.AddUid(uid);

        if (Buffer.Length != 0)
            clone.Buffer.Write(Buffer.Data.Span);
        if (Buffer.IsCompressed)
            clone.Buffer.MarkCompressed();

        return clone;
    }
}
