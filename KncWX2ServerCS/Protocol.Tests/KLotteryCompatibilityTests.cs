using KncWX2Server.Common;

internal static class KLotteryCompatibilityTests
{
    public static void Run()
    {
        TestAddCaseAndParameters();
        TestProbabilityOperations();
        TestDecisionAndSeed();
    }

    private static void TestAddCaseAndParameters()
    {
        var lottery = new KLottery();

        Assert(lottery.AddCase(10, 25, 1, 2));
        Assert(lottery.AddCase(20, 50, 3, 4));
        Assert(lottery.AddCase(10, 10, 5, 6));
        Assert(!lottery.AddCase(30, 20.000002));

        Assert(lottery.CaseCount == 2);
        Assert(Math.Abs(lottery.TotalProbability - 85) < 0.000001);
        Assert(lottery.GetParam1(10) == 5);
        Assert(lottery.GetParam2(10) == 6);
        Assert(lottery.GetParam1(99) == KLottery.ParamBlank);
        Assert(lottery.GetFirstCase() == 10);
        Assert(lottery.IsExistCase(20));
    }

    private static void TestProbabilityOperations()
    {
        var lottery = new KLottery();
        Assert(lottery.AddCase(1, 20));
        Assert(lottery.AddCase(2, 30));
        Assert(lottery.AddProbability(10));
        Assert(Math.Abs(lottery.TotalProbability - 70) < 0.000001);
        Assert(Math.Abs(lottery.Cases[1].Probability - 30) < 0.000001);
        Assert(lottery.DeleteProbability(1));
        Assert(Math.Abs(lottery.TotalProbability - 40) < 0.000001);

        // Native MakeHundredProbabillty() currently refuses to run below 100%.
        Assert(!lottery.MakeHundredProbability());
        Assert(Math.Abs(lottery.TotalProbability - 40) < 0.000001);

        lottery.Clear();
        Assert(lottery.CaseCount == 0);
        Assert(lottery.TotalProbability == 0);
    }

    private static void TestDecisionAndSeed()
    {
        var first = new KLottery();
        first.AddCase(1, 50);
        first.AddCase(2, 50);

        KLottery.Seed(42);
        var firstDecision = first.Decision(out var roulette);
        Assert(roulette > 0 && roulette <= 100);
        Assert(firstDecision is 1 or 2);

        var second = new KLottery();
        second.AddCase(1, 50);
        second.AddCase(2, 50);
        KLottery.Seed(42);
        Assert(firstDecision == second.Decision());
    }

    private static void Assert(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("KLottery compatibility assertion failed.");
    }
}
