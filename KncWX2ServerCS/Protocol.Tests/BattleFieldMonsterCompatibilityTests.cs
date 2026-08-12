using KncWX2Server.Runtime.BattleField;

internal static class BattleFieldMonsterCompatibilityTests
{
    public static void Run()
    {
        TestLifecycleAndStartedCount();
        TestGroupZeroDoesNotRespawn();
        TestGroupedMonsterRespawnReservation();
        TestDeathClassification();
        TestAttribAndOwnerState();
        TestSnapshots();
    }

    private static BattleFieldMonsterInfo Monster(int uid, int groupId, long owner = 0, bool attrib = false) =>
        new(uid, uid + 100, groupId, 10, owner, attrib);

    private static void TestLifecycleAndStartedCount()
    {
        var manager = new BattleFieldMonsterManager();
        manager.StartGame([Monster(1, 10), Monster(2, 20)]);

        Assert(manager.AliveMonsterCount == 2);
        Assert(manager.AtStartedMonsterCount == 2);

        manager.EndGame();
        Assert(manager.AliveMonsterCount == 0);
        Assert(manager.AtStartedMonsterCount == 0);
        Assert(manager.RespawnReservations.Count == 0);
    }

    private static void TestGroupZeroDoesNotRespawn()
    {
        var manager = new BattleFieldMonsterManager();
        manager.StartGame([Monster(1, 0)]);
        Assert(manager.SetMonsterType(1, BattleFieldMonsterTypeFactor.NormalNpc));
        Assert(manager.SetMonsterDie(1, 100, 3, DateTimeOffset.UtcNow));

        Assert(manager.AliveMonsterCount == 0);
        Assert(manager.RespawnReservations.Count == 0);
        Assert(manager.NormalNpcDieCount == 1);
    }

    private static void TestGroupedMonsterRespawnReservation()
    {
        var manager = new BattleFieldMonsterManager();
        var at = new DateTimeOffset(2026, 8, 12, 2, 0, 0, TimeSpan.Zero);
        manager.StartGame([Monster(10, 55)]);
        Assert(manager.SetMonsterType(10, BattleFieldMonsterTypeFactor.LowEliteNpc));
        Assert(manager.SetMonsterDie(10, 200, 3, at));

        Assert(manager.RespawnReservations.Count == 1);
        Assert(manager.TryGetMonsterGroupId(10, out var groupId) && groupId == 55);
        Assert(manager.GetRespawnReadyNpcUids(at.AddSeconds(3)).Count == 0);
        Assert(manager.GetRespawnReadyNpcUids(at.AddSeconds(3.01)).SequenceEqual([10]));
        Assert(manager.GetRespawnReadyGroupCounts(at.AddSeconds(3.01))[55] == 1);
        Assert(manager.ConsumeRespawnReservation(10));
        Assert(manager.RespawnReservations.Count == 0);
        Assert(manager.LowEliteNpcDieCount == 1);
    }

    private static void TestDeathClassification()
    {
        var manager = new BattleFieldMonsterManager();
        var monsters = new[] { Monster(1, 1), Monster(2, 2), Monster(3, 3), Monster(4, 4), Monster(5, 5) };
        manager.StartGame(monsters);

        Assert(manager.SetMonsterType(1, BattleFieldMonsterTypeFactor.NormalNpc));
        Assert(manager.SetMonsterType(2, BattleFieldMonsterTypeFactor.LowEliteNpc));
        Assert(manager.SetMonsterType(3, BattleFieldMonsterTypeFactor.HighEliteNpc));
        Assert(manager.SetMonsterType(4, BattleFieldMonsterTypeFactor.MiddleBossNpc));
        Assert(manager.SetMonsterType(5, BattleFieldMonsterTypeFactor.BossNpc));

        var now = DateTimeOffset.UtcNow;
        foreach (var uid in new[] { 1, 2, 3, 4, 5 })
        {
            Assert(manager.SetMonsterDie(uid, 999, 0, now));
        }

        Assert(manager.NormalNpcDieCount == 1);
        Assert(manager.LowEliteNpcDieCount == 1);
        Assert(manager.HighEliteNpcDieCount == 1);
        Assert(manager.MiddleBossDieCount == 1);
        Assert(manager.BossDieCount == 1);
    }

    private static void TestAttribAndOwnerState()
    {
        var manager = new BattleFieldMonsterManager();
        manager.StartGame([Monster(77, 9, 500)]);

        Assert(!manager.IsAttribNpc(77));
        Assert(manager.SetAttribMonster(77));
        Assert(manager.IsAttribNpc(77));

        Assert(manager.TryGetNpcOwner(77, out var owner) && owner == 500);
        Assert(manager.GetNpcOwnerListByUnitUid(500).SequenceEqual([77]));
        manager.SetNpcOwner(78, 500);
        Assert(manager.GetNpcOwnerListByUnitUid(500).SequenceEqual([77, 78]));

        Assert(manager.SetMonsterDie(77, 500, 1, DateTimeOffset.UtcNow));
        Assert(!manager.TryGetNpcOwner(77, out _));
        Assert(manager.GetNpcOwnerListByUnitUid(500).SequenceEqual([78]));
    }

    private static void TestSnapshots()
    {
        var manager = new BattleFieldMonsterManager();
        manager.StartGame([Monster(30, 3), Monster(10, 1), Monster(20, 2)]);

        Assert(manager.GetAliveMonsterSnapshot().Select(static monster => monster.NpcUid).SequenceEqual([10, 20, 30]));
        Assert(manager.TryGetMonsterInfo(20, out var monster) && monster!.NpcId == 120);

        manager.IncreaseMonsterDieCount(BattleFieldMonsterTypeFactor.NormalNpc);
        manager.IncreaseMonsterDieCount(BattleFieldMonsterTypeFactor.BossNpc);
        Assert(manager.DieCounts == new BattleFieldMonsterDieCounts(1, 0, 0, 0, 1));
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("BattleField monster compatibility assertion failed");
        }
    }
}
