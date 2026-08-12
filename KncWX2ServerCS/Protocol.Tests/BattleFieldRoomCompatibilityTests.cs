using KncWX2Server.Runtime.BattleField;

internal static class BattleFieldRoomCompatibilityTests
{
    public static void Run()
    {
        TestRoomManagerLifecycleAndJoinableSelection();
        TestRoomUnitAndHostLifecycle();
        TestDangerousManagerIntegration();
        TestMonsterManagerIntegration();
    }

    private static void TestRoomManagerLifecycleAndJoinableSelection()
    {
        var manager = new BattleFieldRoomManager();
        var config = new BattleFieldDangerousConfig(10, 1000, 100, 1);
        var first = manager.Create("First", 4, 11, config);
        var second = manager.Create("Second", 4, 11, config);

        Assert(manager.Count == 2);
        Assert(first.RoomUid < second.RoomUid);
        Assert(manager.FindJoinable(11, 1)?.RoomUid == first.RoomUid);
        Assert(manager.FindJoinable(12, 1) is null);

        Assert(manager.Remove(second.RoomUid));
        Assert(manager.Count == 1);
        Assert(!manager.Remove(second.RoomUid));
    }

    private static void TestRoomUnitAndHostLifecycle()
    {
        var manager = new BattleFieldRoomManager();
        var room = manager.Create("Battle", 2, 7);
        Assert(room.OpenSlot(0));
        Assert(room.OpenSlot(1));

        Assert(manager.AddUnit(room.RoomUid, 100, out var firstSlot));
        Assert(manager.AddUnit(room.RoomUid, 200, out var secondSlot));
        Assert(firstSlot == 0 && secondSlot == 1);
        Assert(room.HostUnitUid == 100);

        Assert(manager.RemoveUnit(room.RoomUid, 100));
        Assert(room.HostUnitUid == 200);
        Assert(!manager.RemoveUnit(room.RoomUid, 100));
    }

    private static void TestDangerousManagerIntegration()
    {
        var config = new BattleFieldDangerousConfig(
            DangerousValueWarning: 25,
            DangerousValueMax: 100,
            EliteMonsterDropValue: 50,
            BossCheckUserCount: 1,
            DangerousValueEventRate: 1.0f);
        var manager = new BattleFieldRoomManager();
        var room = manager.Create("Danger", 1, 7, config);

        room.GameManager.IncreaseDangerousValue(25);
        Assert(room.GameManager.DangerousValue == 25);

        room.StartGame();
        Assert(room.GameManager.DangerousValue == 0);

        room.GameManager.IncreaseDangerousValue(80);
        room.EndGame();
        Assert(room.GameManager.DangerousValue == 0);
    }

    private static void TestMonsterManagerIntegration()
    {
        var manager = new BattleFieldRoomManager();
        var room = manager.Create("Monster", 1, 7);
        var at = new DateTimeOffset(2026, 8, 12, 3, 0, 0, TimeSpan.Zero);
        var monster = new BattleFieldMonsterInfo(500, 9000, 12, 15, 0, false);

        room.StartGame([monster]);
        Assert(room.MonsterManager.AliveMonsterCount == 1);
        Assert(room.MonsterManager.AtStartedMonsterCount == 1);
        Assert(room.MonsterManager.SetMonsterType(500, BattleFieldMonsterTypeFactor.BossNpc));
        Assert(room.MonsterManager.SetMonsterDie(500, 100, 5, at));
        Assert(room.MonsterManager.BossDieCount == 1);
        Assert(room.MonsterManager.GetRespawnReadyNpcUids(at.AddSeconds(5.01)).SequenceEqual([500]));

        room.OnCloseRoom();
        Assert(room.State == RoomState.Closed);
        Assert(room.MonsterManager.AliveMonsterCount == 0);
        Assert(room.MonsterManager.RespawnReservations.Count == 0);
        Assert(room.GameManager.DangerousValue == 0);
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("BattleField room compatibility assertion failed");
        }
    }
}
