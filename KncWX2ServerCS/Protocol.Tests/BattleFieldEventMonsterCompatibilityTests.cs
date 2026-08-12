using KncWX2Server.Runtime.BattleField;

internal static class BattleFieldEventMonsterCompatibilityTests
{
    public static void Run()
    {
        TestEventClassification();
        TestMonsterLifecycleAndRespawn();
        TestEventEndCleanup();
    }

    private static BattleFieldEventMonsterInfo Create(int uid, int eventId = 10) =>
        new(uid, 2000 + uid, eventId, 30, 40, true, false, false, 0);

    private static void TestEventClassification()
    {
        var manager = new BattleFieldEventMonsterManager();
        Assert(manager.StartEvent(10));
        Assert(manager.StartEvent(20));

        Assert(manager.ClassifyStartedEvents([10, 20, 30]).SequenceEqual([30]));
        Assert(manager.ClassifyEndedEvents([10]).SequenceEqual([20]));
    }

    private static void TestMonsterLifecycleAndRespawn()
    {
        var manager = new BattleFieldEventMonsterManager();
        Assert(manager.StartEvent(10));
        Assert(manager.AddMonster(Create(100)));
        Assert(manager.IsEventMonsterAlive(100));

        var now = DateTimeOffset.UtcNow;
        Assert(manager.SetMonsterDie(100, 5, now));
        Assert(!manager.IsEventMonsterAlive(100));
        Assert(manager.IsEventMonster(100));
        Assert(manager.GetRespawnReadyNpcUids(now.AddSeconds(5)).Count == 0);
        Assert(manager.GetRespawnReadyNpcUids(now.AddSeconds(5.001)).SequenceEqual([100]));
        Assert(manager.ConsumeRespawnReservation(100));
    }

    private static void TestEventEndCleanup()
    {
        var manager = new BattleFieldEventMonsterManager();
        Assert(manager.StartEvent(10));
        Assert(manager.AddMonster(Create(100)));
        Assert(manager.AddMonster(Create(101)));
        Assert(manager.SetMonsterDie(101, 5));

        var removed = new List<int>();
        Assert(manager.EndEvent(10, removed));
        Assert(removed.SequenceEqual([100]));
        Assert(!manager.IsEventMonster(100));
        Assert(!manager.IsEventMonster(101));
        Assert(manager.RespawnReservations.Count == 0);
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("BattleField event-monster compatibility assertion failed");
        }
    }
}
