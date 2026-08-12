namespace KncWX2Server.Runtime.BattleField;

public enum BattleFieldMonsterTypeFactor
{
    NormalNpc = 0,
    LowEliteNpc = 1,
    HighEliteNpc = 2,
    MiddleBossNpc = 3,
    BossNpc = 4,
}

public sealed record BattleFieldMonsterInfo(
    int NpcUid,
    int NpcId,
    int GroupId,
    int Level,
    long OwnerUnitUid,
    bool IsAttribNpc);

public sealed record BattleFieldMonsterRespawnInfo(
    int MonsterGroupId,
    double RespawnSeconds,
    DateTimeOffset ReservedAtUtc)
{
    public bool IsRespawnTimeOver(DateTimeOffset nowUtc) =>
        nowUtc - ReservedAtUtc > TimeSpan.FromSeconds(RespawnSeconds);
}

/// <summary>
/// Dependency-light state counterpart of native KBattleFieldMonsterManager.
/// NPC packet/table generation remains injectable and outside this core state machine.
/// </summary>
public sealed class BattleFieldMonsterManager
{
    private readonly Dictionary<int, BattleFieldMonsterInfo> _aliveMonsters = [];
    private readonly Dictionary<int, BattleFieldMonsterRespawnInfo> _respawnReservations = [];
    private readonly Dictionary<int, long> _npcOwners = [];
    private readonly Dictionary<int, BattleFieldMonsterTypeFactor> _monsterTypes = [];

    public int AtStartedMonsterCount { get; private set; }
    public int NormalNpcDieCount { get; private set; }
    public int LowEliteNpcDieCount { get; private set; }
    public int HighEliteNpcDieCount { get; private set; }
    public int MiddleBossDieCount { get; private set; }
    public int BossDieCount { get; private set; }

    public IReadOnlyDictionary<int, BattleFieldMonsterInfo> AliveMonsters => _aliveMonsters;
    public IReadOnlyDictionary<int, BattleFieldMonsterRespawnInfo> RespawnReservations => _respawnReservations;
    public int AliveMonsterCount => _aliveMonsters.Count;

    public void StartGame(IEnumerable<BattleFieldMonsterInfo>? initialMonsters = null)
    {
        ClearRuntimeState();

        if (initialMonsters is not null)
        {
            foreach (var monster in initialMonsters)
            {
                CreateMonster(monster);
            }
        }

        AtStartedMonsterCount = AliveMonsterCount;
    }

    public void EndGame() => ClearRuntimeState();
    public void OnCloseRoom() => ClearRuntimeState();

    public bool CreateMonster(BattleFieldMonsterInfo monster)
    {
        ArgumentNullException.ThrowIfNull(monster);
        if (monster.NpcUid == 0 || !_aliveMonsters.TryAdd(monster.NpcUid, monster))
        {
            return false;
        }

        if (monster.OwnerUnitUid != 0)
        {
            _npcOwners[monster.NpcUid] = monster.OwnerUnitUid;
        }

        return true;
    }

    public bool SetMonsterType(int npcUid, BattleFieldMonsterTypeFactor type)
    {
        if (!_aliveMonsters.ContainsKey(npcUid))
        {
            return false;
        }

        _monsterTypes[npcUid] = type;
        return true;
    }

    public bool SetAttribMonster(int npcUid, bool isAttribNpc = true)
    {
        if (!_aliveMonsters.TryGetValue(npcUid, out var info) || info.IsAttribNpc == isAttribNpc)
        {
            return false;
        }

        _aliveMonsters[npcUid] = info with { IsAttribNpc = isAttribNpc };
        return true;
    }

    public bool IsAttribNpc(int npcUid) =>
        _aliveMonsters.TryGetValue(npcUid, out var info) && info.IsAttribNpc;

    public bool IsMonsterAlive(int npcUid) => _aliveMonsters.ContainsKey(npcUid);

    public bool SetMonsterDie(
        int npcUid,
        long attackerUnitUid,
        double respawnSeconds,
        DateTimeOffset? nowUtc = null)
    {
        if (!_aliveMonsters.Remove(npcUid, out var monster))
        {
            return false;
        }

        _npcOwners.Remove(npcUid);

        if (_monsterTypes.Remove(npcUid, out var monsterType))
        {
            IncreaseMonsterDieCount(monsterType);
        }

        // Native behavior: GroupID == 0 means it is not a server-created monster,
        // therefore no respawn reservation is recorded.
        if (monster.GroupId != 0)
        {
            _respawnReservations[npcUid] = new BattleFieldMonsterRespawnInfo(
                monster.GroupId,
                Math.Max(0, respawnSeconds),
                nowUtc ?? DateTimeOffset.UtcNow);
        }

        return true;
    }

    public bool TryGetMonsterGroupId(int npcUid, out int groupId)
    {
        if (_aliveMonsters.TryGetValue(npcUid, out var monster))
        {
            groupId = monster.GroupId;
            return true;
        }

        if (_respawnReservations.TryGetValue(npcUid, out var respawn))
        {
            groupId = respawn.MonsterGroupId;
            return true;
        }

        groupId = 0;
        return false;
    }

    public void IncreaseMonsterDieCount(BattleFieldMonsterTypeFactor type)
    {
        switch (type)
        {
            case BattleFieldMonsterTypeFactor.NormalNpc:
                NormalNpcDieCount++;
                break;
            case BattleFieldMonsterTypeFactor.LowEliteNpc:
                LowEliteNpcDieCount++;
                break;
            case BattleFieldMonsterTypeFactor.HighEliteNpc:
                HighEliteNpcDieCount++;
                break;
            case BattleFieldMonsterTypeFactor.MiddleBossNpc:
                MiddleBossDieCount++;
                break;
            case BattleFieldMonsterTypeFactor.BossNpc:
                BossDieCount++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public IReadOnlyList<int> GetRespawnReadyNpcUids(DateTimeOffset? nowUtc = null)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        return _respawnReservations
            .Where(pair => pair.Value.IsRespawnTimeOver(now))
            .Select(pair => pair.Key)
            .OrderBy(static id => id)
            .ToArray();
    }

    public IReadOnlyDictionary<int, int> GetRespawnReadyGroupCounts(DateTimeOffset? nowUtc = null)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        return _respawnReservations
            .Where(pair => pair.Value.IsRespawnTimeOver(now))
            .GroupBy(pair => pair.Value.MonsterGroupId)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    public bool ConsumeRespawnReservation(int npcUid) => _respawnReservations.Remove(npcUid);

    public void SetNpcOwner(int npcUid, long ownerUnitUid)
    {
        if (!_npcOwners.ContainsKey(npcUid))
        {
            _npcOwners.Add(npcUid, ownerUnitUid);
        }
    }

    public bool EraseNpcOwner(int npcUid) => _npcOwners.Remove(npcUid);

    public IReadOnlyList<int> GetNpcOwnerListByUnitUid(long ownerUnitUid) =>
        _npcOwners
            .Where(pair => pair.Value == ownerUnitUid)
            .Select(pair => pair.Key)
            .OrderBy(static id => id)
            .ToArray();

    public bool TryGetNpcOwner(int npcUid, out long ownerUnitUid) =>
        _npcOwners.TryGetValue(npcUid, out ownerUnitUid);

    public void ClearRuntimeState()
    {
        _aliveMonsters.Clear();
        _respawnReservations.Clear();
        _npcOwners.Clear();
        _monsterTypes.Clear();
        AtStartedMonsterCount = 0;
        NormalNpcDieCount = 0;
        LowEliteNpcDieCount = 0;
        HighEliteNpcDieCount = 0;
        MiddleBossDieCount = 0;
        BossDieCount = 0;
    }
}
