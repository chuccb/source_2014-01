namespace KncWX2Server.Runtime.BattleField;

public sealed record BattleFieldMiddleBossInfo(
    int NpcUid,
    int NpcId,
    int GroupId,
    int BossGroupId,
    int Level,
    bool IsActive,
    bool IsNoDrop,
    bool IsBoss,
    int MonsterGrade);

/// <summary>
/// State-only counterpart of the native middle-boss portion of KBattleFieldMonsterManager.
/// Data-table lookup and packet construction remain outside this class.
/// </summary>
public sealed class BattleFieldMiddleBossManager
{
    private readonly Dictionary<int, BattleFieldMiddleBossInfo> _alive = [];
    private readonly List<List<BattleFieldMiddleBossInfo>> _clientGroups = [];

    public int AliveCount => _alive.Count;
    public bool HasAliveMiddleBoss => _alive.Count != 0;
    public IReadOnlyDictionary<int, BattleFieldMiddleBossInfo> Alive => _alive;
    public IReadOnlyList<IReadOnlyList<BattleFieldMiddleBossInfo>> ClientGroups => _clientGroups;

    public void Clear()
    {
        _alive.Clear();
        _clientGroups.Clear();
    }

    public bool CreateGroup(IEnumerable<BattleFieldMiddleBossInfo> monsters)
    {
        ArgumentNullException.ThrowIfNull(monsters);

        var group = monsters.ToList();
        if (group.Count == 0 || _alive.Count != 0 || group.Any(static monster => monster.NpcUid == 0))
        {
            return false;
        }

        if (group.Any(monster => _alive.ContainsKey(monster.NpcUid)))
        {
            return false;
        }

        foreach (var monster in group)
        {
            _alive.Add(monster.NpcUid, monster);
        }

        _clientGroups.Add(group);
        return true;
    }

    public bool Create(BattleFieldMiddleBossInfo monster)
    {
        ArgumentNullException.ThrowIfNull(monster);
        if (_alive.Count != 0 || monster.NpcUid == 0)
        {
            return false;
        }

        _alive.Add(monster.NpcUid, monster);
        _clientGroups.Add([monster]);
        return true;
    }

    public bool IsMiddleBossMonster(int npcUid) =>
        _alive.ContainsKey(npcUid) || _clientGroups.Any(group => group.Any(monster => monster.NpcUid == npcUid));

    public bool IsMiddleBossMonsterAlive(int npcUid) => _alive.ContainsKey(npcUid);

    public bool TryGet(int npcUid, out BattleFieldMiddleBossInfo? info) =>
        _alive.TryGetValue(npcUid, out info);

    public bool SetMiddleBossMonsterDie(int npcUid)
    {
        if (!_alive.Remove(npcUid))
        {
            return false;
        }

        for (var groupIndex = 0; groupIndex < _clientGroups.Count; groupIndex++)
        {
            var group = _clientGroups[groupIndex];
            var filtered = group.Where(monster => monster.NpcUid != npcUid).ToList();
            if (filtered.Count == group.Count)
            {
                continue;
            }

            _clientGroups[groupIndex] = filtered;
            break;
        }

        return true;
    }

    public IReadOnlyList<IReadOnlyList<BattleFieldMiddleBossInfo>> SnapshotGroups() =>
        _clientGroups.Select(static group => (IReadOnlyList<BattleFieldMiddleBossInfo>)group.ToArray()).ToArray();
}
