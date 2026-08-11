namespace KncWX2Server.Runtime.BattleField;

public enum DangerousEvent
{
    WarningMessage = 0,
    EliteMonsterDrop = 1,
    MiddleBossMonsterDrop = 2,
    BossMonsterDrop = 3,
}

public sealed class DangerousEventInfo
{
    private readonly HashSet<DangerousEvent> _reserved = [];

    public IReadOnlySet<DangerousEvent> ReservedEvents => _reserved;

    public bool IsEventReserved(DangerousEvent eventId) => _reserved.Contains(eventId);

    public bool ReserveEvent(DangerousEvent eventId) => _reserved.Add(eventId);

    public bool DeleteEvent(DangerousEvent eventId) => _reserved.Remove(eventId);

    public void Clear() => _reserved.Clear();
}

public sealed record BattleFieldDangerousConfig(
    int DangerousValueWarning,
    int DangerousValueMax,
    int EliteMonsterDropValue,
    int BossCheckUserCount,
    Func<int, int, float>? BossMonsterDropRate = null,
    Func<int, int, float>? MiddleBossMonsterDropRate = null,
    float DangerousValueEventRate = 1.0f)
{
    public static BattleFieldDangerousConfig Default { get; } = new(
        DangerousValueWarning: int.MaxValue,
        DangerousValueMax: int.MaxValue,
        EliteMonsterDropValue: 0,
        BossCheckUserCount: int.MaxValue);
}

/// <summary>
/// Native KBattleFieldGameManager parity for the dependency-free dangerous-value state.
/// Data-table driven thresholds and lottery decisions stay injectable because the native
/// values come from CXSLBattleFieldManager/KLottery.
/// </summary>
public sealed class BattleFieldGameManager
{
    private readonly BattleFieldDangerousConfig _config;
    private readonly DangerousEventInfo _dangerousEvent = new();

    public BattleFieldGameManager(BattleFieldDangerousConfig? config = null) =>
        _config = config ?? BattleFieldDangerousConfig.Default;

    public int DangerousValue { get; private set; }
    public int OldDangerousValue { get; private set; }
    public DangerousEventInfo DangerousEvent => _dangerousEvent;

    public void StartGame()
    {
        ResetDangerousValue();
        _dangerousEvent.Clear();
    }

    public void EndGame()
    {
        ResetDangerousValue();
        _dangerousEvent.Clear();
    }

    public void ResetDangerousValue()
    {
        DangerousValue = 0;
        OldDangerousValue = 0;
    }

    public void IncreaseDangerousValue(int increaseValue)
    {
        OldDangerousValue = DangerousValue;

        var scaled = increaseValue * _config.DangerousValueEventRate;
        DangerousValue = checked((int)(DangerousValue + scaled));

        if (DangerousValue < 0)
        {
            DangerousValue = 0;
            OldDangerousValue = 0;
        }
        else if (DangerousValue >= _config.DangerousValueMax)
        {
            DangerousValue = 0;
        }
    }

    public void OnNpcUnitDie(
        int playerCount,
        bool isAttribNpc,
        char difficultyLevel,
        char monsterGrade,
        Func<bool, char, char, int> monsterTypeFactor,
        Func<float, bool> lotteryDecision,
        bool increaseDanger = true,
        bool enableMiddleBoss = false)
    {
        ArgumentNullException.ThrowIfNull(monsterTypeFactor);
        ArgumentNullException.ThrowIfNull(lotteryDecision);

        var beforeDangerousValue = DangerousValue;
        var monsterFactor = monsterTypeFactor(isAttribNpc, difficultyLevel, monsterGrade);

        if (!increaseDanger)
        {
            return;
        }

        IncreaseDangerousValue(monsterFactor);
        CheckReserveWarningEvent(beforeDangerousValue);
        CheckReserveEliteMonsterDropEvent(beforeDangerousValue, lotteryDecision);

        if (enableMiddleBoss)
        {
            CheckReserveMiddleBossDropEvent(playerCount, lotteryDecision);
        }

        CheckReserveBossDropEvent(playerCount, lotteryDecision);
    }

    public bool CheckAndDeleteReservedDangerousEvent(DangerousEvent eventId)
    {
        return _dangerousEvent.DeleteEvent(eventId);
    }

    private void CheckReserveWarningEvent(int beforeDangerousValue)
    {
        if (beforeDangerousValue < _config.DangerousValueWarning &&
            DangerousValue >= _config.DangerousValueWarning)
        {
            _dangerousEvent.ReserveEvent(DangerousEvent.WarningMessage);
        }
    }

    private void CheckReserveEliteMonsterDropEvent(int beforeDangerousValue, Func<float, bool> lotteryDecision)
    {
        var threshold = _config.EliteMonsterDropValue;
        if (threshold <= 0 || DangerousValue < threshold)
        {
            return;
        }

        var isMultiple = DangerousValue % threshold == 0;
        var isGap = DangerousValue - beforeDangerousValue > threshold;
        if (!isMultiple && !isGap)
        {
            return;
        }

        if (lotteryDecision(0.20f))
        {
            _dangerousEvent.ReserveEvent(DangerousEvent.EliteMonsterDrop);
        }
    }

    private void CheckReserveMiddleBossDropEvent(int playerCount, Func<float, bool> lotteryDecision)
    {
        if (playerCount < _config.BossCheckUserCount ||
            DangerousValue < _config.DangerousValueWarning ||
            _dangerousEvent.IsEventReserved(DangerousEvent.MiddleBossMonsterDrop))
        {
            return;
        }

        var rate = _config.MiddleBossMonsterDropRate?.Invoke(DangerousValue, OldDangerousValue) ?? 0.0f;
        if (lotteryDecision(rate))
        {
            _dangerousEvent.ReserveEvent(DangerousEvent.MiddleBossMonsterDrop);
        }
    }

    private void CheckReserveBossDropEvent(int playerCount, Func<float, bool> lotteryDecision)
    {
        if (playerCount < _config.BossCheckUserCount ||
            DangerousValue < _config.DangerousValueWarning ||
            _dangerousEvent.IsEventReserved(DangerousEvent.BossMonsterDrop))
        {
            return;
        }

        var rate = _config.BossMonsterDropRate?.Invoke(DangerousValue, OldDangerousValue) ?? 0.0f;
        if (lotteryDecision(rate))
        {
            _dangerousEvent.ReserveEvent(DangerousEvent.BossMonsterDrop);
        }
    }
}
