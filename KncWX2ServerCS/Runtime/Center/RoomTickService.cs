namespace KncWX2Server.Runtime.Center;

/// <summary>State-dependent work boundary corresponding to KRoom::Tick.</summary>
public sealed class RoomTickService
{
    public sealed record Hooks(Action? OnClose=null,Action? OnTimeCount=null,Action? OnLoading=null,Action? OnPlay=null,Action? OnResult=null,Action? OnReturnToField=null);
    private readonly Hooks _hooks;
    public RoomTickService(Hooks hooks)=>_hooks=hooks;
    public void Tick(CenterRoom room)
    {
        ArgumentNullException.ThrowIfNull(room);
        switch(room.StateMachine.State)
        {
            case RoomState.Close:_hooks.OnClose?.Invoke();break;
            case RoomState.TimeCount:_hooks.OnTimeCount?.Invoke();break;
            case RoomState.Load:_hooks.OnLoading?.Invoke();break;
            case RoomState.Play:_hooks.OnPlay?.Invoke();break;
            case RoomState.Result:_hooks.OnResult?.Invoke();break;
            case RoomState.ReturnToField:_hooks.OnReturnToField?.Invoke();break;
        }
    }
}
