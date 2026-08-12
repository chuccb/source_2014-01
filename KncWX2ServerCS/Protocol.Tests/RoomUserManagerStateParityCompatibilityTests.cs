using KncWX2Server.Runtime.Center;

internal static class RoomUserManagerStateParityCompatibilityTests
{
    public static void Run()
    {
        TestIndexAndCidLookup();
        TestTeamReadyAndHostState();
        TestStageHpAndDungeonState();
        TestObserverIsolation();
        TestKillScoreSnapshot();
    }

    private static RoomUser CreateUser(long cid, int team = 0)
    {
        var user = new RoomUser
        {
            UnitUid = cid,
            GSUid = cid + 1000,
            PartyUid = cid + 2000,
        };
        user.SetTeam(team);
        return user;
    }

    private static void TestIndexAndCidLookup()
    {
        var manager = new RoomUserManager();
        manager.Init(3, 1);

        var first = CreateUser(300);
        var second = CreateUser(100);
        var third = CreateUser(200);
        Assert(manager.EnterRoom(first));
        Assert(manager.EnterRoom(second));
        Assert(manager.EnterRoom(third));

        Assert(manager.GetUserByIndex(0)?.Cid == 100);
        Assert(manager.GetUserByIndex(1)?.Cid == 200);
        Assert(manager.GetUserByIndex(2)?.Cid == 300);
        Assert(manager.GetUserByIndex(3) is null);
        Assert(manager.GetUserByIndex(-1) is null);

        Assert(manager.GetGsUid(200, out var gsUid) && gsUid == 1200);
        Assert(!manager.GetGsUid(999, out _));
        Assert(manager.IsHost(300));
    }

    private static void TestTeamReadyAndHostState()
    {
        var manager = new RoomUserManager();
        manager.Init(4);
        var red = CreateUser(10, 0);
        var blue = CreateUser(20, 1);
        var red2 = CreateUser(30, 0);
        var blue2 = CreateUser(40, 1);

        Assert(manager.EnterRoom(red));
        Assert(manager.EnterRoom(blue));
        Assert(manager.EnterRoom(red2));
        Assert(manager.EnterRoom(blue2));

        Assert(manager.SetReady(10, true));
        Assert(manager.SetReady(30, true));
        Assert(manager.GetTeamReadyNum(0) == 2);
        Assert(manager.GetTeamReadyNum(1) == 0);
        Assert(manager.GetTeamNum(out var redCount, out var blueCount));
        Assert(redCount == 2 && blueCount == 2);

        manager.SetAllReady(true);
        Assert(manager.GetTeamReadyNum(1) == 2);
        Assert(manager.IsHost(10));
        Assert(manager.IsPlaying(10) == false);
    }

    private static void TestStageHpAndDungeonState()
    {
        var manager = new RoomUserManager();
        manager.Init(2);
        var one = CreateUser(10);
        var two = CreateUser(20);
        Assert(manager.EnterRoom(one));
        Assert(manager.EnterRoom(two));

        one.StartGame();
        two.StartGame();
        one.StartPlay();
        two.StartPlay();

        Assert(!manager.IsAllPlayerHpReported());
        one.SetHP(100);
        two.SetHP(200);
        Assert(manager.IsAllPlayerHpReported());

        Assert(!manager.IsAllPlayerStageId());
        one.SetStage(1);
        two.SetStage(2);
        Assert(manager.IsAllPlayerStageId());

        Assert(!manager.IsAllPlayerDungeonUnitInfoSet());
        one.SetDungeonUnitInfoReceived(true);
        two.SetDungeonUnitInfoReceived(true);
        Assert(manager.IsAllPlayerDungeonUnitInfoSet());

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
        Assert(manager.EnterRoom(observer, observer: true));

        Assert(manager.IsObserver(20));
        Assert(!manager.IsObserver(10));
        Assert(manager.GetUser(20, RoomUserManager.UserListType.Game) is null);
        Assert(manager.GetUser(20, RoomUserManager.UserListType.Observer) is not null);
        Assert(manager.GetRoomUserGs(20, out var gsUid) && gsUid == 1020);
    }

    private static void TestKillScoreSnapshot()
    {
        var manager = new RoomUserManager();
        manager.Init(2);
        var first = CreateUser(20);
        var second = CreateUser(10);
        Assert(manager.EnterRoom(first));
        Assert(manager.EnterRoom(second));

        first.IncreaseKill();
        first.IncreaseKill();
        first.IncreaseDie();
        first.IncreaseMDKill();
        second.IncreaseKill();

        var snapshot = manager.GetCurrentKillScore();
        Assert(snapshot.Count == 2);
        Assert(snapshot[0] == (10, 1, 0, 0));
        Assert(snapshot[1] == (20, 2, 1, 1));
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("RoomUserManager state parity assertion failed");
        }
    }
}
