using KncWX2Server.Runtime.BattleField;

internal static class BattleFieldDangerousCompatibilityTests
{
    public static void Run()
    {
        TestLifecycleReset();
        TestDangerousValueClampAndWrap();
        TestWarningThreshold();
        TestEliteDropReservationAndDelete();
        TestBossReservations();
        TestNoIncreaseMeansNoEvents();
    }

    private static void TestLifecycleReset()
    {
        var manager = new BattleFieldGameManager(new BattleFieldDangerousConfig(10, 100, 20, 1));
        manager.IncreaseDangerousValue(7);
        manager.DangerousEvent.ReserveEvent(DangerousEvent.BossMonsterDrop);

        manager.StartGame();

        Assert(manager.DangerousValue == 0);
        Assert(manager.OldDangerousValue == 0);
        Assert(!manager.DangerousEvent.IsEventReserved(DangerousEvent.BossMonsterDrop));
    }

    private static void TestDangerousValueClampAndWrap()
    {
        var manager = new BattleFieldGameManager(new BattleFieldDangerousConfig(10, 100, 20, 1));
        manager.IncreaseDangerousValue(35);
        Assert(manager.DangerousValue == 35 && manager.OldDangerousValue == 0);

        manager.IncreaseDangerousValue(-50);
        Assert(manager.DangerousValue == 0 && manager.OldDangerousValue == 0);

        manager.IncreaseDangerousValue(100);
        Assert(manager.DangerousValue == 0);
    }

    private static void TestWarningThreshold()
    {
        var manager = new BattleFieldGameManager(new BattleFieldDangerousConfig(50, 1000, 100, 99));
        manager.OnNpcUnitDie(
            playerCount: 1,
            isAttribNpc: false,
            difficultyLevel: (char)0,
            monsterGrade: (char)0,
            monsterTypeFactor: static (_, _, _) => 40,
            lotteryDecision: static _ => false);
        Assert(!manager.DangerousEvent.IsEventReserved(DangerousEvent.WarningMessage));

        manager.OnNpcUnitDie(
            playerCount: 1,
            isAttribNpc: false,
            difficultyLevel: (char)0,
            monsterGrade: (char)0,
            monsterTypeFactor: static (_, _, _) => 10,
            lotteryDecision: static _ => false);
        Assert(manager.DangerousValue == 50);
        Assert(manager.DangerousEvent.IsEventReserved(DangerousEvent.WarningMessage));
    }

    private static void TestEliteDropReservationAndDelete()
    {
        var manager = new BattleFieldGameManager(new BattleFieldDangerousConfig(200, 1000, 50, 99));
        manager.IncreaseDangerousValue(50);

        manager.OnNpcUnitDie(
            1,
            false,
            (char)0,
            (char)0,
            static (_, _, _) => 10,
            static rate => Math.Abs(rate - 0.20f) < 0.0001f);

        Assert(manager.DangerousValue == 60);
        Assert(!manager.DangerousEvent.IsEventReserved(DangerousEvent.EliteMonsterDrop));

        manager.OnNpcUnitDie(
            1,
            false,
            (char)0,
            (char)0,
            static (_, _, _) => 40,
            static rate => Math.Abs(rate - 0.20f) < 0.0001f);

        Assert(manager.DangerousValue == 100);
        Assert(manager.DangerousEvent.IsEventReserved(DangerousEvent.EliteMonsterDrop));
        Assert(manager.CheckAndDeleteReservedDangerousEvent(DangerousEvent.EliteMonsterDrop));
        Assert(!manager.DangerousEvent.IsEventReserved(DangerousEvent.EliteMonsterDrop));
    }

    private static void TestBossReservations()
    {
        var manager = new BattleFieldGameManager(new BattleFieldDangerousConfig(
            DangerousValueWarning: 50,
            DangerousValueMax: 1000,
            EliteMonsterDropValue: 100,
            BossCheckUserCount: 2,
            BossMonsterDropRate: static (_, _) => 0.30f,
            MiddleBossMonsterDropRate: static (_, _) => 0.40f));

        manager.IncreaseDangerousValue(60);
        manager.OnNpcUnitDie(
            playerCount: 2,
            isAttribNpc: false,
            difficultyLevel: (char)0,
            monsterGrade: (char)0,
            monsterTypeFactor: static (_, _, _) => 1,
            lotteryDecision: static rate => rate >= 0.30f,
            enableMiddleBoss: true);

        Assert(manager.DangerousEvent.IsEventReserved(DangerousEvent.MiddleBossMonsterDrop));
        Assert(manager.DangerousEvent.IsEventReserved(DangerousEvent.BossMonsterDrop));

        Assert(manager.CheckAndDeleteReservedDangerousEvent(DangerousEvent.MiddleBossMonsterDrop));
        Assert(manager.CheckAndDeleteReservedDangerousEvent(DangerousEvent.BossMonsterDrop));
    }

    private static void TestNoIncreaseMeansNoEvents()
    {
        var manager = new BattleFieldGameManager(new BattleFieldDangerousConfig(10, 100, 10, 1));
        manager.OnNpcUnitDie(
            10,
            false,
            (char)0,
            (char)0,
            static (_, _, _) => 100,
            static _ => true,
            increaseDanger: false);

        Assert(manager.DangerousValue == 0);
        Assert(manager.DangerousEvent.ReservedEvents.Count == 0);
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("BattleField dangerous-value compatibility assertion failed");
        }
    }
}
