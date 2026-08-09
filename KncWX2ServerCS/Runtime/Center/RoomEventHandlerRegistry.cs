namespace KncWX2Server.Runtime.Center;

public sealed class RoomEventHandlerRegistry
{
    private readonly Dictionary<ushort,Func<RoomEvent,ValueTask>> _handlers=new();
    public void Register(ushort eventId,Func<RoomEvent,ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if(!_handlers.TryAdd(eventId,handler))throw new InvalidOperationException($"Room event handler already registered: {eventId}");
    }
    public bool TryDispatch(RoomEvent value,out ValueTask task)
    {
        if(_handlers.TryGetValue(value.EventId,out var handler)){task=handler(value);return true;}
        task=ValueTask.CompletedTask;return false;
    }
    public int Count=>_handlers.Count;
}
