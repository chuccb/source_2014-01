namespace KncWX2Server.Runtime.BattleField;

public sealed record BattleFieldEventMonsterInfo(
    int NpcUid,
    int NpcId,
    int EventId,
    int GroupId,
    int Level,
    bool IsActive,
    bool IsNoDrop,
    bool IsBoss,
    int MonsterGrade);

public sealed record BattleFieldEventRespawnInfo(
    int EventId,
    double RespawnSeconds,
    DateTimeOffset ReservedAtUtc)
{
    public bool IsRespawnTimeOver(DateTimeOffset nowUtc) =>
        nowUtc - ReservedAtUtc > TimeSpan.FromSeconds(RespawnSeconds);
}

/// <summary>
/// Dependency-light state counterpart of native field-event monster tracking.
/// Event script selection and NPC packet/data-table generation remain external.
/// </summary>
public sealed class BattleFieldEventMonsterManager
{
    private readonly Dictionary<int, HashSet<int>> _eventMonsters = [];
    private readonly Dictionary<int, BattleFieldEventMonsterInfo> _alive = [];
    private readonly Dictionary<int, BattleFieldEventMonsterInfo> _dead = [];
    private readonly Dictionary<int, BattleFieldEventRespawnInfo> _respawn = [];

    public IReadOnlyDictionary<int, HashSet<int>> EventMonsters => _eventMonsters;
    public IReadOnlyDictionary<int, BattleFieldEventMonsterInfo> Alive => _alive;
    public IReadOnlyDictionary<int, BattleFieldEventMonsterInfo> Dead => _dead;
    public IReadOnlyDictionary<int, BattleFieldEventRespawnInfo> RespawnReservations => _respawn;

    public void Clear()
    {
        _eventMonsters.Clear();
        _alive.Clear();
        _dead.Clear();
        _respawn.Clear();
    }

    public IReadOnlyList<int> ClassifyStartedEvents(IEnumerable<int> runningEventIds)
    {
        ArgumentNullException.ThrowIfNull(runningEventIds);
        var running = runningEventIds.ToHashSet();
        return running.Where(id => !_eventMonsters.ContainsKey(id)).OrderBy(static id => id).ToArray();
    }

    public IReadOnlyList<int> ClassifyEndedEvents(IEnumerable<int> runningEventIds)
    {
        ArgumentNullException.ThrowIfNull(runningEventIds);
        var running = runningEventIds.ToHashSet();
        return _eventMonsters.Keys.Where(id => !running.Contains(id)).OrderBy(static id => id).ToArray();
    }

    public bool StartEvent(int eventId)
    {
        if (_eventMonsters.ContainsKey(eventId))
        {
            return false;
        }

        _eventMonsters.Add(eventId, []);
        return true;
    }

    public bool AddMonster(BattleFieldEventMonsterInfo monster)
    {
        ArgumentNullException.ThrowIfNull(monster);
        if (monster.NpcUid == 0 || !_eventMonsters.TryGetValue(monster.EventId, out var ids))
        {
            return false;
        }

        if (!_alive.TryAdd(monster.NpcUid, monster))
        {
            return false;
        }

        _dead.Remove(monster.NpcUid);
        _respawn.Remove(monster.NpcUid);
        ids.Add(monster.NpcUid);
        return true;
    }

    public bool SetMonsterDie(int npcUid, double respawnSeconds, DateTimeOffset? nowUtc = null)
    {
        if (!_alive.Remove(npcUid, out var monster))
        {
            return false;
        }

        _dead[npcUid] = monster;
        if (monster.EventId != 0)
        {
            _respawn[npcUid] = new BattleFieldEventRespawnInfo(
                monster.EventId,
                Math.Max(0, respawnSeconds),
                nowUtc ?? DateTimeOffset.UtcNow);
        }

        return true;
    }

    public bool IsEventMonster(int npcUid) => _alive.ContainsKey(npcUid) || _dead.ContainsKey(npcUid);
    public bool IsEventMonsterAlive(int npcUid) => _alive.ContainsKey(npcUid);

    public bool TryGet(int npcUid, out BattleFieldEventMonsterInfo? info) =>
        _alive.TryGetValue(npcUid, out info) || _dead.TryGetValue(npcUid, out info);

    public bool EndEvent(int eventId, ICollection<int>? removedAliveNpcUids = null)
    {
        if (!_eventMonsters.Remove(eventId, out var npcIds))
        {
            return false;
        }

        foreach (var npcUid in npcIds)
        {
            if (_alive.Remove(npcUid))
            {
                removedAliveNpcUids?.Add(npcUid);
            }

            _dead.Remove(npcUid);
            _respawn.Remove(npcUid);
        }

        return true;
    }

    public IReadOnlyList<int> GetRespawnReadyNpcUids(DateTimeOffset? nowUtc = null)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        return _respawn
            .Where(pair => pair.Value.IsRespawnTimeOver(now))
            .Select(pair => pair.Key)
            .OrderBy(static id => id)
            .ToArray();
    }

    public bool ConsumeRespawnReservation(int npcUid) => _respawn.Remove(npcUid);
}
