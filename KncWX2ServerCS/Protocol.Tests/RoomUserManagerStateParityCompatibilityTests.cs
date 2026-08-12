using KncWX2Server.Runtime.Center;

internal static class RoomUserManagerStateParityCompatibilityTests
{
    public static void Run()
    {
        TestUserStateTransitions();
        TestManagerFlagsAndReset();
        TestObserverIsolation();
        TestKillScoreSnapshot();
    }

    private static RoomUser CreateUser(long uid)
    {
        return new RoomUser
        {
            UnitUid = uid,
            UserUid = 1000 + uid,
            GSUid = 2000 + uid,
            Cid = uid,
        };
    }

    private static void TestUserStateTransitions()
    {
        var user = CreateUser(1);
        Assert(user.StateMachine.State == RoomUserState.Init);
        Assert(user.StateMachine.Send(RoomUserInput.ToLoad));
        Assert(user.StateMachine.State == RoomUserState.Load);
        Assert(user.StateMachine.Send(RoomUserInput.ToPlay));
        Assert(user.StateMachine.State == RoomUserState.Play);
        Assert(user.StateMachine.Send(RoomUserInput.ToResult));
        Assert(user.StateMachine.State == RoomUserState.Result);
        Assert(user.StateMachine.Send(RoomUserInput.ToInit));
        Assert(user.StateMachine.State == RoomUserState.Init);
    }

    private static void TestManagerFlagsAndReset()
    {
        var manager = new RoomUserManager();
        manager.Init(2);
        var one = CreateUser(1);
        var two = CreateUser(2);
        Assert(manager.EnterRoom(one));
        Assert(manager.EnterRoom(two));

        Assert(manager.SetReady(1, true));
        Assert(manager.SetLoadingProgress(1, 100));
        Assert(manager.SetStageLoaded(1, true));
        Assert(manager.SetDie(1, true));
        Assert(manager.SetHP(1, 0));
        Assert(manager.SetStageId(1, 3));
        Assert(manager.SetSubStageId(1, 4));
        Assert(manager.SetRebirthPos(1, 5));
        Assert(one.IsReady && one.LoadingProgress == 100 && one.IsStageLoaded && one.IsDie);

        manager.ResetStageLoaded();
        manager.ResetStageId();
        manager.ResetRebirthPos();
        Assert(one.StageId == -1 && two.StageId == -1);
        Assert(one.SubStageId == -1 && two.SubStageId == -1);
        Assert(one.RebirthPos == 0 && two.RebirthPos == 0);
        Assert(!one.IsStageLoaded && !two.IsStageLoaded);
    }

    private static void TestObserverIsolation()
    {
        var manager = new RoomUserManager();
        manager.Init(1, 1);
        var game = CreateUser(10);
        var observer = CreateUser(20);
        Assert(manager.EnterRoom(game));
        Assert(manager.EnterRoom(observer, type: RoomUserManager.UserListType.Observer));

        Assert(manager.IsObserver(20));
        Assert(!manager.IsObserver(10));
        Assert(manager.GetUser(20, RoomUserManager.UserListType.Game) is null);
        Assert(manager.GetUser(20, RoomUserManager.UserListType.Observer) is not null);
        Assert(manager.GetRoomUserGs(20, out var gsUid) && gsUid == 2020);
    }

    private static void TestKillScoreSnapshot()
    {
        var manager = new RoomUserManager();
        manager.Init(2);
        var first = CreateUser(20);
        var second = CreateUser(21);
        Assert(manager.EnterRoom(first));
        Assert(manager.EnterRoom(second));
        Assert(manager.IncreaseNumKill(first.UnitUid));
        Assert(manager.IncreaseNumKill(first.UnitUid));
        Assert(manager.IncreaseTeamNumKill(first.UnitUid));
        Assert(manager.GetMaxKillUnit() == 2);
        Assert(manager.GetMaxKillTeam() == 1);
        Assert(manager.GetTeamScore(first.Team) == 1);
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("RoomUserManager state parity assertion failed");
        }
    }
}
