using KncWX2Server.Runtime.BattleField;

internal static class BattleFieldMiddleBossCompatibilityTests
{
    public static void Run()
    {
        TestSingleGroupLifecycle();
        TestCannotCreateSecondAliveGroup();
        TestGroupRemovalKeepsOtherMembers();
        TestReset();
    }

    private static BattleFieldMiddleBossInfo Create(int uid, int group = 7, int bossGroup = 9) =>
        new(uid, 1000 + uid, group, bossGroup, 50, true, false, true, 3);

    private static void TestSingleGroupLifecycle()
    {
        var manager = new BattleFieldMiddleBossManager();
        Assert(manager.CreateGroup([Create(1), Create(2)]));
        Assert(manager.AliveCount == 2);
        Assert(manager.HasAliveMiddleBoss);
        Assert(manager.IsMiddleBossMonster(1));
        Assert(manager.IsMiddleBossMonsterAlive(2));
        Assert(manager.TryGet(1, out var info) && info?.BossGroupId == 9);
    }

    private static void TestCannotCreateSecondAliveGroup()
    {
        var manager = new BattleFieldMiddleBossManager();
        Assert(manager.Create(Create(1)));
        Assert(!manager.Create(Create(2)));
        Assert(manager.AliveCount == 1);
    }

    private static void TestGroupRemovalKeepsOtherMembers()
    {
        var manager = new BattleFieldMiddleBossManager();
        Assert(manager.CreateGroup([Create(1), Create(2), Create(3)]));
        Assert(manager.SetMiddleBossMonsterDie(2));
        Assert(!manager.IsMiddleBossMonsterAlive(2));
        Assert(manager.IsMiddleBossMonster(1));
        Assert(manager.IsMiddleBossMonster(3));
        Assert(manager.ClientGroups.Count == 1);
        Assert(manager.ClientGroups[0].Count == 2);
        Assert(!manager.SetMiddleBossMonsterDie(99));
    }

    private static void TestReset()
    {
        var manager = new BattleFieldMiddleBossManager();
        Assert(manager.CreateGroup([Create(1), Create(2)]));
        manager.Clear();
        Assert(manager.AliveCount == 0);
        Assert(!manager.HasAliveMiddleBoss);
        Assert(manager.ClientGroups.Count == 0);
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("BattleField middle-boss compatibility assertion failed");
        }
    }
}
