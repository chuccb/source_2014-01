using KncWX2Server.Runtime.BattleField;

internal static class BattleFieldRoomStateMachineCompatibilityTests
{
    public static void Run()
    {
        TestNativeTransitionGraph();
        TestInvalidTransitionIsRejected();
        TestForceAndEvent();
    }

    private static void TestNativeTransitionGraph()
    {
        var machine = new RoomStateMachine();

        Require(machine.State == RoomState.Init, "initial state must be Init");
        Require(machine.Send(RoomInput.ToWait), "Init -> Wait");
        Require(machine.Send(RoomInput.ToTimeCount), "Wait -> TimeCount");
        Require(machine.Send(RoomInput.ToLoad), "TimeCount -> Load");
        Require(machine.Send(RoomInput.ToPlay), "Load -> Play");
        Require(machine.Send(RoomInput.ToResult), "Play -> Result");
        Require(machine.Send(RoomInput.ToWait), "Result -> Wait");
        Require(machine.Send(RoomInput.ToReturnToField), "Wait -> ReturnToField");
        Require(machine.Send(RoomInput.ToClose), "ReturnToField -> Closed");
        Require(machine.Send(RoomInput.ToInit), "Closed -> Init");
        Require(machine.State == RoomState.Init, "graph must end at Init");
    }

    private static void TestInvalidTransitionIsRejected()
    {
        var machine = new RoomStateMachine();
        Require(!machine.Send(RoomInput.ToPlay), "Init -> Play must be rejected");
        Require(machine.State == RoomState.Init, "rejected transition must preserve state");
    }

    private static void TestForceAndEvent()
    {
        var machine = new RoomStateMachine();
        var fired = new List<(RoomState From, RoomState To)>();
        machine.Transitioned += (from, to) => fired.Add((from, to));

        machine.Force(RoomState.Play);
        machine.Force(RoomState.Play);
        Require(fired.Count == 1, "Force to same state must not raise a duplicate transition");
        Require(fired[0] == (RoomState.Init, RoomState.Play), "transition event payload mismatch");
        Require(machine.State == RoomState.Play, "Force must update state");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"BattleField RoomStateMachine vector failed: {message}");
        }
    }
}
