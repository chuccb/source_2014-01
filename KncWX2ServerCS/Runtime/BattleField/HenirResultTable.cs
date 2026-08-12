namespace KncWX2Server.Runtime.BattleField;

/// <summary>Identifies a Henir reward table entry by stage count and dungeon mode.</summary>
public readonly record struct HenirRewardKey(int StageCount, byte DungeonMode);

/// <summary>Describes one reward-group selection attached to a Henir table entry.</summary>
public readonly record struct HenirRewardDefinition(int GroupId, int RandomCount);

/// <summary>Defines one deterministic challenge reward before item metadata is resolved.</summary>
public readonly record struct HenirChallengeReward(int ItemId, int Quantity);

/// <summary>
/// Managed counterpart of native KHenirResultTable.
///
/// The native table owns resource/configuration state. Random selection and item
/// metadata resolution are intentionally kept outside this type until their native
/// KLottery and XSL item contracts are ported.
/// </summary>
public sealed class HenirResultTable
{
    private readonly Dictionary<HenirRewardKey, List<HenirRewardDefinition>> _rewardDefinitions = [];
    private readonly Dictionary<int, List<HenirChallengeReward>> _challengeRewards = [];
    private readonly HashSet<int> _resurrectionStages = [];
    private readonly HashSet<int> _clearTempInventoryStages = [];
    private readonly HashSet<int> _clearNotifyStages = [];

    public int RewardTableCount => _rewardDefinitions.Count;
    public int ResurrectionStageCount => _resurrectionStages.Count;
    public int ClearTempInventoryStageCount => _clearTempInventoryStages.Count;
    public int ClearNotifyStageCount => _clearNotifyStages.Count;
    public int ChallengeStageCount => _challengeRewards.Count;

    public bool AddHenirResultItemInfo(
        int stageCount,
        byte dungeonMode,
        int itemGroupId,
        int randomCount)
    {
        if (stageCount <= 0 || itemGroupId <= 0 || randomCount <= 0)
            return false;

        var key = new HenirRewardKey(stageCount, dungeonMode);
        _rewardDefinitions.GetOrAdd(key).Add(new HenirRewardDefinition(itemGroupId, randomCount));
        return true;
    }

    public bool AddResurrectionStage(int stageCount) =>
        stageCount >= 0 && _resurrectionStages.Add(stageCount);

    public bool AddClearTempInventoryStage(int stageId) =>
        stageId >= 0 && _clearTempInventoryStages.Add(stageId);

    public bool AddClearNotifyStage(int stageCount) =>
        stageCount >= 0 && _clearNotifyStages.Add(stageCount);

    public bool AddChallengeReward(int stageId, int itemId, int quantity)
    {
        if (stageId < 0 || itemId < 0 || quantity <= 0)
            return false;

        var rewards = _challengeRewards.GetOrAdd(stageId);
        if (rewards.Any(reward => reward.ItemId == itemId))
            return false;

        rewards.Add(new HenirChallengeReward(itemId, quantity));
        return true;
    }

    public bool IsResurrectionStage(int stageCount) => _resurrectionStages.Contains(stageCount);

    public bool IsClearTempInventoryStage(int stageId) => _clearTempInventoryStages.Contains(stageId);

    public bool IsClearNotifyStage(int stageCount) => _clearNotifyStages.Contains(stageCount);

    public bool TryGetRewardDefinitions(
        int stageCount,
        byte dungeonMode,
        out IReadOnlyList<HenirRewardDefinition> rewards)
    {
        if (_rewardDefinitions.TryGetValue(new HenirRewardKey(stageCount, dungeonMode), out var found))
        {
            rewards = found;
            return true;
        }

        rewards = [];
        return false;
    }

    public bool TryGetChallengeRewards(
        int stageId,
        out IReadOnlyList<HenirChallengeReward> rewards)
    {
        if (_challengeRewards.TryGetValue(stageId, out var found))
        {
            rewards = found;
            return true;
        }

        rewards = [];
        return false;
    }

    public void Clear()
    {
        _rewardDefinitions.Clear();
        _challengeRewards.Clear();
        _resurrectionStages.Clear();
        _clearTempInventoryStages.Clear();
        _clearNotifyStages.Clear();
    }
}

file static class DictionaryExtensions
{
    public static List<TValue> GetOrAdd<TKey, TValue>(
        this Dictionary<TKey, List<TValue>> dictionary,
        TKey key)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var values))
            return values;

        values = [];
        dictionary.Add(key, values);
        return values;
    }
}
