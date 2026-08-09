namespace KncWX2Server.Runtime.BattleField;

public sealed class RoomStateMachine
{
    public RoomState State { get; private set; } = RoomState.Init;
    public event Action<RoomState,RoomState>? Transitioned;
    public bool TryTransition(RoomState next){if(!IsAllowed(State,next))return false;var previous=State;State=next;Transitioned?.Invoke(previous,next);return true;}
    public void Force(RoomState state){var previous=State;State=state;if(previous!=state)Transitioned?.Invoke(previous,state);}
    private static bool IsAllowed(RoomState from,RoomState to)=>from switch
    {
        RoomState.Init=>to is RoomState.Closed or RoomState.Wait,
        RoomState.Closed=>to is RoomState.Wait,
        RoomState.Wait=>to is RoomState.TimeCount or RoomState.Closed or RoomState.Loading,
        RoomState.TimeCount=>to is RoomState.Loading or RoomState.Wait or RoomState.Closed,
        RoomState.Loading=>to is RoomState.Play or RoomState.Wait or RoomState.Closed,
        RoomState.Play=>to is RoomState.Result or RoomState.ReturnToField or RoomState.Closed,
        RoomState.Result=>to is RoomState.ReturnToField or RoomState.Wait or RoomState.Closed,
        RoomState.ReturnToField=>to is RoomState.Wait or RoomState.Closed,
        _=>false
    };
}
