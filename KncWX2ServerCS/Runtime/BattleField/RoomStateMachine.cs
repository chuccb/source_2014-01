namespace KncWX2Server.Runtime.BattleField;

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
}

/// <summary>
/// Battlefield-specific room state machine.  The transition graph mirrors the
/// native CenterServer room lifecycle while using the Battlefield RoomState names.
/// </summary>
public sealed class RoomStateMachine
{
    public RoomState State { get; private set; } = RoomState.Init;

    public event Action<RoomState, RoomState>? Transitioned;

    public bool TryTransition(RoomState nextState)
    {
        if (!CanTransition(State, nextState))
        {
            return false;
        }

        TransitionTo(nextState);
        return true;
    }

    public bool Send(RoomInput input)
    {
        var nextState = (State, input) switch
        {
            (RoomState.Init, RoomInput.ToWait) => RoomState.Wait,
            (RoomState.Closed, RoomInput.ToInit) => RoomState.Init,
            (RoomState.Wait, RoomInput.ToClose) => RoomState.Closed,
            (RoomState.Wait, RoomInput.ToLoad) => RoomState.Loading,
            (RoomState.Wait, RoomInput.ToTimeCount) => RoomState.TimeCount,
            (RoomState.Wait, RoomInput.ToReturnToField) => RoomState.ReturnToField,
            (RoomState.TimeCount, RoomInput.ToClose) => RoomState.Closed,
            (RoomState.TimeCount, RoomInput.ToLoad) => RoomState.Loading,
            (RoomState.Loading, RoomInput.ToClose) => RoomState.Closed,
            (RoomState.Loading, RoomInput.ToPlay) => RoomState.Play,
            (RoomState.Play, RoomInput.ToClose) => RoomState.Closed,
            (RoomState.Play, RoomInput.ToResult) => RoomState.Result,
            (RoomState.Result, RoomInput.ToClose) => RoomState.Closed,
            (RoomState.Result, RoomInput.ToWait) => RoomState.Wait,
            (RoomState.ReturnToField, RoomInput.ToClose) => RoomState.Closed,
            (RoomState.ReturnToField, RoomInput.ToWait) => RoomState.Wait,
            _ => (RoomState?)null,
        };

        return nextState is { } resolvedState && TryTransition(resolvedState);
    }

    public void Force(RoomState state) => TransitionTo(state);

    private static bool CanTransition(RoomState current, RoomState next) =>
        (current, next) switch
        {
            (RoomState.Init, RoomState.Wait) => true,
            (RoomState.Closed, RoomState.Init) => true,
            (RoomState.Wait, RoomState.Closed or RoomState.TimeCount or RoomState.Loading or RoomState.ReturnToField) => true,
            (RoomState.TimeCount, RoomState.Closed or RoomState.Loading) => true,
            (RoomState.Loading, RoomState.Closed or RoomState.Play) => true,
            (RoomState.Play, RoomState.Closed or RoomState.Result) => true,
            (RoomState.Result, RoomState.Closed or RoomState.Wait) => true,
            (RoomState.ReturnToField, RoomState.Closed or RoomState.Wait) => true,
            _ => false,
        };

    private void TransitionTo(RoomState state)
    {
        var previousState = State;
        if (previousState == state)
        {
            return;
        }

        State = state;
        Transitioned?.Invoke(previousState, state);
    }
}
