using KncWX2Server.Runtime.BattleField;

internal static class HenirResultTableCompatibilityTests
{
    public static void Run()
    {
        TestRewardDefinitions();
        TestRewardGroups();
        TestStageFlags();
        TestChallengeRewards();
        TestClear();
    }

    private static void TestRewardDefinitions()
    {
        var table = new HenirResultTable();

        Assert(!table.AddHenirResultItemInfo(0, 0, 1, 1));
        Assert(!table.AddHenirResultItemInfo(1, 0, 0, 1));
        Assert(!table.AddHenirResultItemInfo(1, 0, 1, 0));
        Assert(table.AddHenirResultItemInfo(1, 2, 10, 3));
        Assert(table.AddHenirResultItemInfo(1, 2, 11, 2));
        Assert(table.AddHenirResultItemInfo(1, 3, 12, 1));

        Assert(table.RewardTableCount == 2);
        Assert(table.TryGetRewardDefinitions(1, 2, out var rewards));
        Assert(rewards.Count == 2);
        Assert(rewards[0] == new HenirRewardDefinition(10, 3));
        Assert(rewards[1] == new HenirRewardDefinition(11, 2));
        Assert(!table.TryGetRewardDefinitions(9, 2, out _));
    }

    private static void TestRewardGroups()
    {
        var table = new HenirResultTable();

        Assert(!table.AddHenirResultItemGroup(0, 1, 1, 1));
        Assert(!table.AddHenirResultItemGroup(1, -1, 1, 1));
        Assert(!table.AddHenirResultItemGroup(1, 1, 0, 1));
        Assert(!table.AddHenirResultItemGroup(1, 1, 1, 0));
        Assert(table.AddHenirResultItemGroup(1, 100, 2, 0.25f));
        Assert(table.AddHenirResultItemGroup(1, 101, 1, 0.75f));
        Assert(!table.AddHenirResultItemGroup(1, 102, 1, 0.01f));

        Assert(table.RewardGroupCount == 1);
        Assert(table.TryGetRewardGroup(1, out var lottery));
        Assert(lottery.CaseCount == 2);
        Assert(Math.Abs(lottery.TotalProbability - 1.0) < 0.000001);
        Assert(lottery.GetParam1(100) == 2);
        Assert(lottery.GetParam1(101) == 1);
        Assert(!table.TryGetRewardGroup(99, out _));
    }

    private static void TestStageFlags()
    {
        var table = new HenirResultTable();

        Assert(table.AddResurrectionStage(0));
        Assert(!table.AddResurrectionStage(-1));
        Assert(!table.AddResurrectionStage(0));
        Assert(table.IsResurrectionStage(0));

        Assert(table.AddClearTempInventoryStage(3));
        Assert(!table.AddClearTempInventoryStage(-1));
        Assert(table.IsClearTempInventoryStage(3));

        Assert(table.AddClearNotifyStage(5));
        Assert(!table.AddClearNotifyStage(-1));
        Assert(table.IsClearNotifyStage(5));

        Assert(table.ResurrectionStageCount == 1);
        Assert(table.ClearTempInventoryStageCount == 1);
        Assert(table.ClearNotifyStageCount == 1);
    }

    private static void TestChallengeRewards()
    {
        var table = new HenirResultTable();

        Assert(!table.AddChallengeReward(-1, 1, 1));
        Assert(!table.AddChallengeReward(1, -1, 1));
        Assert(!table.AddChallengeReward(1, 1, 0));
        Assert(table.AddChallengeReward(1, 100, 2));
        Assert(!table.AddChallengeReward(1, 100, 3));
        Assert(table.AddChallengeReward(1, 101, 4));

        Assert(table.ChallengeStageCount == 1);
        Assert(table.TryGetChallengeRewards(1, out var rewards));
        Assert(rewards.Count == 2);
        Assert(rewards[0] == new HenirChallengeReward(100, 2));
        Assert(rewards[1] == new HenirChallengeReward(101, 4));
    }

    private static void TestClear()
    {
        var table = new HenirResultTable();
        Assert(table.AddHenirResultItemInfo(1, 1, 10, 1));
        Assert(table.AddHenirResultItemGroup(10, 100, 1, 1));
        Assert(table.AddResurrectionStage(1));
        Assert(table.AddClearTempInventoryStage(1));
        Assert(table.AddClearNotifyStage(1));
        Assert(table.AddChallengeReward(1, 10, 1));

        table.Clear();

        Assert(table.RewardTableCount == 0);
        Assert(table.RewardGroupCount == 0);
        Assert(table.ResurrectionStageCount == 0);
        Assert(table.ClearTempInventoryStageCount == 0);
        Assert(table.ClearNotifyStageCount == 0);
        Assert(table.ChallengeStageCount == 0);
    }

    private static void Assert(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("HenirResultTable compatibility assertion failed.");
    }
}
