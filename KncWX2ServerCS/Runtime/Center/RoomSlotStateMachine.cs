namespace KncWX2Server.Runtime.Center;

public enum RoomSlotState
{
    Init = 0,
    Closed = 1,
    Assigned = 2,
}

public enum RoomSlotInput
{
    ToInit = 0,
    ToClosed = 1,
    ToAssigned = 2,
}

public sealed class RoomSlotStateMachine
{
    public RoomSlotState State { get; private set; } = RoomSlotState.Init;

    public event Action<RoomSlotState, RoomSlotState>? Transitioned;

    public bool Send(RoomSlotInput input)
    {
        var nextState = (State, input) switch
        {
            (RoomSlotState.Init, RoomSlotInput.ToClosed) => RoomSlotState.Closed,
            (RoomSlotState.Init, RoomSlotInput.ToAssigned) => RoomSlotState.Assigned,
            (RoomSlotState.Closed, RoomSlotInput.ToInit) => RoomSlotState.Init,
            (RoomSlotState.Assigned, RoomSlotInput.ToInit) => RoomSlotState.Init,
            _ => (RoomSlotState?)null,
        };

        if (nextState is not { } resolvedState)
        {
            return false;
        }

        var previousState = State;
        State = resolvedState;
        Transitioned?.Invoke(previousState, State);
        return true;
    }

    public void Force(RoomSlotState state)
    {
        var previousState = State;
        State = state;

        if (previousState != state)
        {
            Transitioned?.Invoke(previousState, state);
        }
    }
}