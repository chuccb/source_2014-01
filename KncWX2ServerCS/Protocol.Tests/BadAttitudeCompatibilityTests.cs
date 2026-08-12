using System.Runtime.CompilerServices;
using KncWX2Server.Runtime.Center;

internal static class BadAttitudeCompatibilityTests
{
    public static void Run()
    {
        TestVotingAndForceExit();
        TestRemoveUnitVoteCleanup();
        TestStageAndMonsterScore();
        TestDefenceWaveScore();
        TestDefaultTablePoints();
    }

    private static void TestVotingAndForceExit()
    {
        var table = new KBadAttitudeTable();
        table.AddBadAttitudeCutLinePoint(7, 2);
        table.AddForceExitPoint(7, 2);

        var manager = new BadAttitudeManager(table);
        manager.Init([100, 200, 300], 7);
        manager.IncreaseBadAttitudeOnePoint(100);
        manager.IncreaseBadAttitudeOnePoint(100);

        var bad = new List<long>();
        var forceExit = new List<long>();
        manager.CheckBadAttitudeUnit(bad, forceExit);

        Assert(bad.Count == 1 && bad[0] == 100);
        Assert(forceExit.Count == 0);
        Assert(manager.GetUnitData(100, out var target) && target!.IsBadAttitudeUnit);

        manager.IncreaseBadAttitudeOnePoint(100);
        manager.IncreaseBadAttitudeOnePoint(100);
        manager.IncreaseVoteOnePoint(100, 200);
        manager.IncreaseVoteOnePoint(100, 300);
        manager.CheckBadAttitudeUnit(bad, forceExit);

        Assert(forceExit.Count == 1 && forceExit[0] == 100);
    }

    private static void TestRemoveUnitVoteCleanup()
    {
        var table = new KBadAttitudeTable();
        table.AddBadAttitudeCutLinePoint(1, 1);
        table.AddForceExitPoint(1, 1);

        var manager = new BadAttitudeManager(table);
        manager.Init([1, 2, 3], 1);
        manager.IncreaseBadAttitudeOnePoint(1);

        var bad = new List<long>();
        var forceExit = new List<long>();
        manager.CheckBadAttitudeUnit(bad, forceExit);
        manager.IncreaseVoteOnePoint(1, 2);
        manager.IncreaseVoteOnePoint(1, 3);

        Assert(manager.RemoveUnit(2, out var removed) && removed!.UnitUid == 2);
        Assert(manager.GetUnitData(1, out var target) && target!.VotePoint == 1);
        Assert(!manager.IncreaseVoteOnePoint(1, 2));
    }

    private static void TestStageAndMonsterScore()
    {
        var manager = new BadAttitudeManager();
        manager.Init([10, 20], 0);
        manager.IncreaseSubStageMonsterDieCount(1, 2, (char)5);

        // Native semantics: all members begin at (-1,-1), so IsAllUnitGetScore() is true
        // until one member receives a different stage/sub-stage.
        Assert(manager.IsAllUnitGetScore());

        var updated = manager.SetUnitSubStageInfo(
            10, 1, 2, 99, 2, 0, 0,
            static (_, _, _, monster) => monster > 0 ? 'F' : 'A',
            static (_, _, _, _) => 'A');

        Assert(updated);
        Assert(manager.GetUnitData(10, out var info));
        Assert(info!.RankInfo.Stage == 1 && info.RankInfo.SubStage == 2);
        Assert(info.BadAttitudePoint == 1);
        Assert(!manager.IsAllUnitGetScore());
    }

    private static void TestDefenceWaveScore()
    {
        var manager = new BadAttitudeManager();
        manager.Init([10], 0);
        manager.SetDefenceDungeonWaveId(3);
        manager.IncreaseSubStageMonsterDieCount(2, 99, (char)7, isDefenceDungeon: true);

        var updated = manager.SetUnitSubStageInfo(
            10, 2, 3, 99, 1, 0, 0,
            static (_, _, _, monster) => monster > 0 ? 'F' : 'A',
            static (_, _, _, _) => 'A');

        Assert(updated);
        Assert(manager.GetUnitData(10, out var info) && info!.BadAttitudePoint == 1);
    }

    private static void TestDefaultTablePoints()
    {
        var table = new KBadAttitudeTable();
        Assert(table.GetBadAttitudeCutLinePoint(999) == 1000);
        Assert(table.GetForceExitPoint(999) == 1000);
    }

    private static void Assert(
        bool condition,
        [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"BadAttitudeManager compatibility assertion failed: {expression}");
        }
    }
}