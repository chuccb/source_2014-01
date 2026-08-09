namespace KncWX2Server.Runtime.Center;

/// <summary>Single consumer dispatcher for Center room events. Domain handlers execute on this boundary.</summary>
public sealed class RoomEventDispatcher
{
    private readonly RoomEventQueue _queue;
    private readonly Func<RoomEvent,ValueTask> _handler;
    public RoomEventDispatcher(RoomEventQueue queue,Func<RoomEvent,ValueTask> handler){_queue=queue;_handler=handler;}
    public async ValueTask<int> DrainAsync(CancellationToken cancellationToken=default)
    {
        var count=0;
        while(!cancellationToken.IsCancellationRequested&&_queue.TryDequeue(out var value)){await _handler(value);count++;}
        return count;
    }
}
