namespace KncWX2Server.Runtime.Center;

using System.Diagnostics;

public sealed class RoomUser
{
    public const int DefaultObserverSlotCount = 3;
    public const double LoadingTimeoutSeconds = 60.0;
    public const double TradeRequestTimeoutSeconds = 10.0;
    public const int SecretStageNone = 0;
    public const int SecretStageAgree = 1;

    private readonly Dictionary<long, Stopwatch> _tradeRequests = [];
    private readonly Dictionary<int, int> _receivedItems = [];
    private bool _ready;

    public long GSUid { get; set; }
    public long UserUid { get; set; }
    public long UnitUid { get; set; }
    public long Cid
    {
        get => UnitUid;
        set => UnitUid = value;
    }

    public long PartyUid { get; set; }

    public int Level { get; private set; } = 1;
    public int UnitType { get; private set; }
    public int Team { get; private set; }
    public int SlotId { get; private set; } = -1;

    public bool IsHost { get; private set; }
    public bool IsReady => (IsHost || _ready) && !IsInTrade;
    public bool IsPitIn { get; private set; }
    public bool IsInTrade { get; private set; }
    public bool IsPvpNpc { get; private set; }
    public bool IsBoss { get; private set; }
    public bool IsEnterCashShopUser { get; private set; }
    public bool IsSuccessResult { get; private set; }
    public bool IsGameBang { get; private set; }
    public int PcBangType { get; private set; } = -1;
    public bool HavePet { get; private set; }
    public int AgreeEnterSecretStage { get; private set; }
    public bool IsIntrude { get; private set; }
    public bool IsRingOfPvpRebirth { get; private set; }

    public int RewardEXP { get; private set; }
    public int RewardPartyEXP { get; private set; }
    public int UsedResurrectionStoneCount { get; private set; }

    public int LoadingProgress { get; private set; } = -1;
    public bool IsStageLoaded { get; private set; }
    public int NumKill { get; private set; }
    public int NumMDKill { get; private set; }
    public int NumDie { get; private set; }
    public bool IsDie { get; private set; }
    public float HP { get; private set; } = -1f;
    public int StageId { get; private set; } = -1;
    public int SubStageId { get; private set; } = -1;
    public int RebirthPos { get; private set; }
    public int PassedStageCount { get; private set; }
    public int PassedSubStageCount { get; private set; }
    public int KillNpcCount { get; private set; }
    public bool DungeonUnitInfoReceived { get; private set; }

    public bool IsAcceptRematch { get; private set; }
    public bool IsPrepareForDefence { get; private set; }
    public bool IsRecvEnterPopupReply { get; private set; }
    public bool IsEnterDefenceDungeon { get; private set; }
    public int MatchWaitTime { get; private set; }
    public int AutoPartyWaitTime { get; private set; }
    public int PvpNpcId { get; private set; }

    public long RidingPetUid { get; private set; }
    public ushort RidingPetId { get; private set; }

    public bool BattleFieldNpcLoad { get; private set; }
    public bool BattleFieldNpcSyncSubjects { get; private set; }
    public bool IsHenirReward { get; private set; }
    public bool ReceivedPingScore { get; private set; }
    public bool ZombieAlert { get; private set; }
    public bool EndPlayFlag { get; private set; }
    public bool CashContinueReady { get; private set; }

    public RoomUserStateMachine StateMachine { get; } = new();

    public bool IsPlaying => StateMachine.State is RoomUserState.Load or RoomUserState.Play;
    public bool IsOnlyPlaying => StateMachine.State is RoomUserState.Play;

    public void SetLevel(int level) => Level = level;
    public void SetUnitType(int unitType) => UnitType = unitType;
    public void SetTeam(int team) => Team = team;
    public void SetSlotId(int slotId) => SlotId = slotId;

    public bool SetHost(bool host)
    {
        SetReady(host);
        IsHost = host;
        return true;
    }

    public bool SetReady(bool ready)
    {
        if (ready && IsInTrade)
        {
            return false;
        }

        _ready = ready;
        return true;
    }

    public void SetPitIn(bool value) => IsPitIn = value;

    public void SetTrade(bool value)
    {
        if (value)
        {
            SetReady(false);
        }

        IsInTrade = value;
    }

    public void SetPvpNpc(bool value) => IsPvpNpc = value;
    public void SetPvpNpcId(int value) => PvpNpcId = value;
    public void SetIsBoss(bool value) => IsBoss = value;
    public void SetEnterCashShopUser(bool value) => IsEnterCashShopUser = value;
    public void SetSuccessResult(bool value) => IsSuccessResult = value;
    public void SetGameBang(bool value) => IsGameBang = value;
    public void SetPcBangType(int value) => PcBangType = value;
    public void SetHavePet(bool value) => HavePet = value;
    public void SetAgreeEnterSecretStage(int value) => AgreeEnterSecretStage = value;

    public bool SetIsIntrude(bool value)
    {
        IsIntrude = value;
        return true;
    }

    public void SetRingOfPvpRebirth(bool value) => IsRingOfPvpRebirth = value;
    public void SetRewardEXP(int value) => RewardEXP = value;
    public void SetRewardPartyEXP(int value) => RewardPartyEXP = value;
    public void SetUsedResurrectionStoneCount(int value) => UsedResurrectionStoneCount = value;
    public void IncreaseUsedResurrectionStoneCount() => UsedResurrectionStoneCount++;

    public void SetLoadingProgress(int value) => LoadingProgress = value;
    public void SetStageLoaded(bool value) => IsStageLoaded = value;
    public void IncreaseKill() => NumKill++;
    public void IncreaseMDKill() => NumMDKill++;
    public void IncreaseDie() => NumDie++;
    public void IncreaseKillNpc() => KillNpcCount++;
    public int GetKillNpcCountForLua() => Math.Max(1, KillNpcCount);

    public void SetDie(bool value) => IsDie = value;
    public void SetHP(float value) => HP = value;
    public void SetStage(int value) => StageId = value;
    public void SetSubStage(int value) => SubStageId = value;

    public void SetRebirthPos(int value) => RebirthPos = value;

    public void IncreasePassedStageCount()
    {
        PassedStageCount++;
        PassedSubStageCount = 0;
    }

    public void IncreasePassedSubStageCount() => PassedSubStageCount++;
    public void SetPassedStageCount(int value) => PassedStageCount = value;
    public void SetPassedSubStageCount(int value) => PassedSubStageCount = value;
    public void SetDungeonUnitInfoReceived(bool value) => DungeonUnitInfoReceived = value;

    public void SetRematch(bool value) => IsAcceptRematch = value;
    public void SetPrepareForDefence(bool value) => IsPrepareForDefence = value;
    public void SetRecvEnterPopupReply(bool value) => IsRecvEnterPopupReply = value;
    public void SetEnterDefenceDungeon(bool value) => IsEnterDefenceDungeon = value;
    public void SetMatchWaitTime(int value) => MatchWaitTime = value;
    public void SetAutoPartyWaitTime(int value) => AutoPartyWaitTime = value;

    public void SetRidingPetInfo(long petUid, ushort petId)
    {
        RidingPetUid = petUid;
        RidingPetId = petId;
    }

    public bool SetBattleFieldNpcLoad(bool value)
    {
        BattleFieldNpcLoad = value;
        return true;
    }

    public bool SetBattleFieldNpcSyncSubjects(bool value)
    {
        BattleFieldNpcSyncSubjects = value;
        return true;
    }

    public bool SetHenirReward(bool value)
    {
        IsHenirReward = value;
        return true;
    }

    public bool SetReceivedPingScore(bool value)
    {
        ReceivedPingScore = value;
        return true;
    }

    public bool SetZombieAlert(bool value)
    {
        ZombieAlert = value;
        return true;
    }

    public bool SetEndPlay(bool value)
    {
        EndPlayFlag = value;
        return true;
    }

    public bool SetCashContinueReady(bool value)
    {
        CashContinueReady = value;
        return true;
    }

    public bool RequestTradeTo(long cid)
    {
        if (_tradeRequests.Count == 0 && IsInTrade)
        {
            return false;
        }

        if (_tradeRequests.ContainsKey(cid))
        {
            return false;
        }

        _tradeRequests.Add(cid, Stopwatch.StartNew());
        SetTrade(true);
        return true;
    }

    public bool TradeAcceptedBy(long cid)
    {
        if (!_tradeRequests.ContainsKey(cid))
        {
            return false;
        }

        _tradeRequests.Clear();
        return true;
    }

    public bool TradeRejectedBy(long cid)
    {
        if (!_tradeRequests.Remove(cid))
        {
            return false;
        }

        if (_tradeRequests.Count == 0)
        {
            SetTrade(false);
        }

        return true;
    }

    public bool ExpireTradeRequests()
    {
        var hadRequests = _tradeRequests.Count > 0;

        foreach (var (cid, timer) in _tradeRequests
                     .Where(pair => pair.Value.Elapsed.TotalSeconds > TradeRequestTimeoutSeconds)
                     .ToArray())
        {
            _tradeRequests.Remove(cid);
        }

        if (hadRequests && _tradeRequests.Count == 0)
        {
            SetTrade(false);
            return true;
        }

        return false;
    }

    public void AddItem(int itemId, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        _receivedItems[itemId] = _receivedItems.GetValueOrDefault(itemId) + quantity;
    }

    public int GetItemCount() => _receivedItems.Values.Sum();

    public void StartGame()
    {
        LoadingProgress = 0;
        IsStageLoaded = false;
        StageId = -1;
        SubStageId = -1;
        RebirthPos = 0;
        NumKill = 0;
        NumMDKill = 0;
        NumDie = 0;
        IsDie = false;
        KillNpcCount = 0;
        HP = -1f;
        IsSuccessResult = false;
        RewardEXP = 0;
        RewardPartyEXP = 0;
        UsedResurrectionStoneCount = 0;
        PassedStageCount = 0;
        PassedSubStageCount = 0;
        DungeonUnitInfoReceived = false;
        _receivedItems.Clear();
        BattleFieldNpcLoad = false;
        BattleFieldNpcSyncSubjects = false;
        IsHenirReward = false;
        ReceivedPingScore = false;
        ZombieAlert = false;
        EndPlayFlag = false;
        CashContinueReady = false;
        IsIntrude = false;
        AgreeEnterSecretStage = SecretStageNone;
        IsPrepareForDefence = false;
        IsRecvEnterPopupReply = false;
        IsEnterDefenceDungeon = false;
        StateMachine.Send(RoomUserInput.ToLoad);
    }

    public void StartPlay()
    {
        LoadingProgress = -1;
        NumKill = 0;
        NumMDKill = 0;
        NumDie = 0;
        IsDie = false;
        HP = -1f;
        EndPlayFlag = false;
        StateMachine.Send(RoomUserInput.ToPlay);
    }

    public void EndPlay()
    {
        LoadingProgress = 0;
        IsStageLoaded = false;
        StageId = -1;
        SubStageId = -1;
        RebirthPos = 0;
        EndPlayFlag = true;
        StateMachine.Send(RoomUserInput.ToResult);
    }

    public void EndGame()
    {
        SetReady(false);
        LoadingProgress = -1;
        StateMachine.Send(RoomUserInput.ToInit);
    }

    public int GetPercentHP(int baseHp) =>
        baseHp <= 0 ? 0 : (int)(HP * 100f / baseHp);
}