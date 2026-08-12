using KncWX2Server.Runtime.Center;

internal static class CenterRuntimeCompatibilityTests
{
    public static void Run()
    {
        TestRoomLifecycleAndTeams();
        TestRoomGameLifecycle();
        TestTeamKillObjectives();
        TestSlotLifecycle();
        TestTeamAndResultChecks();
    }

    private static RoomUser CreateUser(long unitUid, int team)
    {
        var user = new RoomUser
        {
            UnitUid = unitUid,
            GSUid = 1000 + unitUid,
        };
        user.SetTeam(team);
        return user;
    }

    private static void TestRoomLifecycleAndTeams()
    {
        var manager = new RoomUserManager();
        manager.Init(4);

        var first = CreateUser(1, 0);
        var second = CreateUser(2, 1);

        Assert(manager.EnterRoom(first, considerTeam: true));
        Assert(manager.EnterRoom(second, considerTeam: true));
        Assert(manager.GetNumMember() == 2);
        Assert(first.IsHost);
        Assert(manager.GetTeamNumPlaying(0) == 0);
        Assert(manager.GetSlot(0)?.IsOccupied == true);
        Assert(manager.GetUnitUIDList().Count == 2);

        Assert(manager.ChangeTeam(2, 0));
        Assert(second.Team == 0);
        Assert(manager.GetTeamMemberList(0).Count == 2);
    }

    private static void TestRoomGameLifecycle()
    {
        var manager = new RoomUserManager();
        manager.Init(2);
        var user = CreateUser(10, 0);
        Assert(manager.EnterRoom(user));
        Assert(manager.SetAllReady(true));
        Assert(manager.IsAllPlayerReady());

        manager.StartGame();
        Assert(user.StateMachine.State == RoomUserState.Load);
        Assert(user.LoadingProgress == 0);

        user.SetStageLoaded(true);
        manager.StartPlay();
        Assert(user.StateMachine.State == RoomUserState.Play);

        Assert(manager.SetStageId(10, 2));
        Assert(manager.SetHP(10, 50));
        Assert(manager.IncreaseNumKill(10));
        Assert(manager.GetMaxKillUnit() == 1);

        manager.StartResult();
        Assert(user.StateMachine.State == RoomUserState.Result);
        manager.EndGame();
        Assert(user.StateMachine.State == RoomUserState.Init);
    }

    private static void TestTeamKillObjectives()
    {
        var manager = new RoomUserManager();
        manager.Init(4);
        var first = CreateUser(20, 0);
        var second = CreateUser(21, 0);
        var third = CreateUser(22, 1);

        Assert(manager.EnterRoom(first));
        Assert(manager.EnterRoom(second));
        Assert(manager.EnterRoom(third));
        manager.SetAllReady(true);
        manager.StartGame();
        manager.StartPlay();

        Assert(manager.IncreaseTeamNumKill(20));
        Assert(manager.IncreaseTeamNumKill(21));
        Assert(manager.GetTeamScore(0) == 2);
        Assert(manager.IsAnyTeamReachObjectiveNumKill(2));
        Assert(manager.GetMaxKillTeam() == 2);
    }

    private static void TestSlotLifecycle()
    {
        var manager = new RoomUserManager();
        manager.Init(4);

        Assert(manager.GetNumTotalSlot() == 4);
        Assert(manager.GetNumOpenedSlot() == 4);
        Assert(manager.CloseSlot(3));
        Assert(manager.GetNumOpenedSlot() == 3);
        Assert(manager.OpenSlot(3));
        Assert(manager.GetNumOpenedSlot() == 4);
        Assert(manager.ToggleOpenClose(3));
        Assert(manager.GetNumOpenedSlot() == 3);
    }

    private static void TestTeamAndResultChecks()
    {
        var manager = new RoomUserManager();
        manager.Init(2);
        var left = CreateUser(30, 0);
        var right = CreateUser(31, 1);
        Assert(manager.EnterRoom(left));
        Assert(manager.EnterRoom(right));
        Assert(manager.SetAllReady(true));
        manager.StartGame();
        manager.StartPlay();

        left.SetDie(true);
        right.SetDie(false);
        Assert(!manager.IsAnyTeamEliminated() || manager.GetNumMember() == 2);
        Assert(!manager.IsAllPlayerAlive());
        Assert(!manager.IsAllPlayerDie());

        manager.StartResult();
        left.SetSuccessResult(true);
        right.SetSuccessResult(true);
        Assert(manager.IsAllPlayerSuccessResult());
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Center runtime compatibility assertion failed");
        }
    }
}