namespace KncWX2Server.Runtime.Center;

public enum RoomUserState
{
    Invalid = 0,
    Init = 1,
    Load = 2,
    Play = 3,
    Result = 4,
}

public enum RoomUserInput
{
    ToInit = 0,
    ToLoad = 1,
    ToPlay = 2,
    ToResult = 3,
}

/// <summary>Behavior-compatible state machine for CenterServer KRoomUserFSM.</summary>
public sealed class RoomUserStateMachine
{
    public RoomUserState State { get; private set; } = RoomUserState.Init;

    public event Action<RoomUserState, RoomUserState>? Transitioned;

    public bool Send(RoomUserInput input)
    {
        var nextState = (State, input) switch
        {
            (RoomUserState.Init, RoomUserInput.ToLoad) => RoomUserState.Load,
            (RoomUserState.Load, RoomUserInput.ToInit) => RoomUserState.Init,
            (RoomUserState.Load, RoomUserInput.ToPlay) => RoomUserState.Play,
            (RoomUserState.Play, RoomUserInput.ToInit) => RoomUserState.Init,
            (RoomUserState.Play, RoomUserInput.ToResult) => RoomUserState.Result,
            (RoomUserState.Result, RoomUserInput.ToInit) => RoomUserState.Init,
            _ => (RoomUserState?)null,
        };

        if (nextState is not { } resolvedState)
        {
            return false;
        }

        TransitionTo(resolvedState);
        return true;
    }

    public void Force(RoomUserState state)
    {
        if (State == state)
        {
            return;
        }

        TransitionTo(state);
    }

    private void TransitionTo(RoomUserState state)
    {
        var previousState = State;
        State = state;
        Transitioned?.Invoke(previousState, state);
    }
}
