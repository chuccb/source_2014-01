using KncWX2Server.Runtime.BattleField;

internal static class BattleFieldRoomCompatibilityTests
{
    public static void Run()
    {
        TestRoomManagerLifecycleAndJoinableSelection();
        TestRoomUnitAndHostLifecycle();
        TestDangerousManagerIntegration();
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

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("BattleField room compatibility assertion failed");
        }
    }
}
