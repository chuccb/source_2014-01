using KncWX2Server.Runtime.Center;

internal static class CenterRoomCompatibilityTests
{
    public static void Run()
    {
        TestHostReassignment();
        TestJoinLeaveSnapshot();
        TestTickDispatch();
    }

    private static RoomUser CreateUser(long unitUid)
    {
        return new RoomUser
        {
            UnitUid = unitUid,
            UserUid = 5000 + unitUid,
            GSUid = 6000 + unitUid,
        };
    }

    private static void TestHostReassignment()
    {
        var room = new CenterRoom(100, 1, 2);
        var first = CreateUser(1);
        var second = CreateUser(2);

        Assert(room.Join(first));
        Assert(first.IsHost);
        Assert(room.Join(second));
        Assert(!second.IsHost);

        Assert(room.LeaveByUnitUid(1));
        Assert(second.IsHost);
        Assert(room.OccupiedCount == 1);
    }

    private static void TestJoinLeaveSnapshot()
    {
        var room = new CenterRoom(101, 2, 3);
        var user = CreateUser(10);
        Assert(room.Join(user));
        Assert(room.FindByUserUid(10_000 + 0) is null);
        Assert(room.FindByUserUid(user.UserUid) == room.FindByUnitUid(user.UnitUid));
        Assert(room.Snapshot().Count(info => info.UnitUid != 0) == 1);
        Assert(room.LeaveByUnitUid(user.UnitUid));
        Assert(room.IsEmpty);
    }

    private static void TestTickDispatch()
    {
        var close = 0;
        var play = 0;
        var result = 0;
        var room = new CenterRoom(102, 3, 1);
        var tick = new RoomTickService(new RoomTickService.Hooks(
            OnClose: () => close++,
            OnPlay: () => play++,
            OnResult: () => result++));

        room.StateMachine.Force(RoomState.Close);
        tick.Tick(room);
        room.StateMachine.Force(RoomState.Play);
        tick.Tick(room);
        room.StateMachine.Force(RoomState.Result);
        tick.Tick(room);

        Assert(close == 1 && play == 1 && result == 1);
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("CenterRoom compatibility assertion failed");
        }
    }
}
