namespace KncWX2Server.Runtime.Center;

public enum RoomState
{
    Invalid = 0,
    Init = 1,
    Close = 2,
    Wait = 3,
    TimeCount = 4,
    Load = 5,
    Play = 6,
    Result = 7,
    ReturnToField = 8,
    WaitForDefence = 9,
}

public enum RoomInput
{
    ToInit = 0,
    ToClose = 1,
    ToWait = 2,
    ToTimeCount = 3,
    ToLoad = 4,
    ToPlay = 5,
    ToResult = 6,
    ToReturnToField = 7,
    ToWaitForDefence = 8,
}

public sealed class RoomStateMachine
{
    public RoomState State { get; private set; } = RoomState.Init;

    public event Action<RoomState, RoomState>? Transitioned;

    public bool Send(RoomInput input)
    {
        var nextState = (State, input) switch
        {
            (RoomState.Init, RoomInput.ToWait) => RoomState.Wait,
            (RoomState.Close, RoomInput.ToInit) => RoomState.Init,
            (RoomState.Wait, RoomInput.ToClose) => RoomState.Close,
            (RoomState.Wait, RoomInput.ToLoad) => RoomState.Load,
            (RoomState.Wait, RoomInput.ToTimeCount) => RoomState.TimeCount,
            (RoomState.Wait, RoomInput.ToReturnToField) => RoomState.ReturnToField,
            (RoomState.Wait, RoomInput.ToWaitForDefence) => RoomState.WaitForDefence,
            (RoomState.TimeCount, RoomInput.ToClose) => RoomState.Close,
            (RoomState.TimeCount, RoomInput.ToLoad) => RoomState.Load,
            (RoomState.Load, RoomInput.ToClose) => RoomState.Close,
            (RoomState.Load, RoomInput.ToPlay) => RoomState.Play,
            (RoomState.Play, RoomInput.ToClose) => RoomState.Close,
            (RoomState.Play, RoomInput.ToResult) => RoomState.Result,
            (RoomState.Result, RoomInput.ToClose) => RoomState.Close,
            (RoomState.Result, RoomInput.ToWait) => RoomState.Wait,
            (RoomState.ReturnToField, RoomInput.ToClose) => RoomState.Close,
            (RoomState.ReturnToField, RoomInput.ToWait) => RoomState.Wait,
            (RoomState.WaitForDefence, RoomInput.ToClose) => RoomState.Close,
            (RoomState.WaitForDefence, RoomInput.ToWait) => RoomState.Wait,
            _ => (RoomState?)null,
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

    public void Force(RoomState state)
    {
        var previousState = State;
        State = state;

        if (previousState != state)
        {
            Transitioned?.Invoke(previousState, state);
        }
    }
}