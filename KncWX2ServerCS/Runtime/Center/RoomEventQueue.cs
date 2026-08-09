using System.Collections.Concurrent;

namespace KncWX2Server.Runtime.Center;

/// <summary>Thread-safe room event queue corresponding to KRoomManager's queueing boundary.</summary>
public sealed class RoomEventQueue
{
    private readonly ConcurrentQueue<RoomEvent> _queue=new();
    public int Count=>_queue.Count;
    public void Enqueue(RoomEvent value)=>_queue.Enqueue(value);
    public bool TryDequeue(out RoomEvent value)=>_queue.TryDequeue(out value!);
    public void Clear(){while(_queue.TryDequeue(out _)){}}
}

public sealed record RoomEvent(ushort EventId,long[] Trace,byte[] Payload)
{
    public long FirstSenderUid=>Trace.Length==0?-1:Trace[0];
    public long LastSenderUid=>Trace.Length>1&&Trace[1]!=-1?Trace[1]:FirstSenderUid;
}
