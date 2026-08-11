namespace KncWX2Server.Runtime.BattleField;

public sealed class BattleFieldRoomManager
{
    private readonly object _gate = new();
    private readonly Dictionary<long, BattleFieldRoom> _rooms = new();
    private long _nextRoomUid = 1;

    public int Count { get { lock (_gate) return _rooms.Count; } }

    public BattleFieldRoom Create(
        string name,
        int maxSlots,
        uint battleFieldId = uint.MaxValue,
        BattleFieldDangerousConfig? dangerousConfig = null)
    {
        if (maxSlots <= 0) throw new ArgumentOutOfRangeException(nameof(maxSlots));
        lock (_gate)
        {
            var uid = _nextRoomUid++;
            var room = new BattleFieldRoom(uid, name, maxSlots, battleFieldId, dangerousConfig);
            room.SetState(RoomState.Wait);
            _rooms.Add(uid, room);
            return room;
        }
    }

    public bool TryGet(long roomUid, out BattleFieldRoom? room)
    {
        lock (_gate) return _rooms.TryGetValue(roomUid, out room);
    }

    public bool Remove(long roomUid)
    {
        lock (_gate) return _rooms.Remove(roomUid);
    }

    public bool AddUnit(long roomUid, long unitUid, out int slotIndex)
    {
        lock (_gate)
        {
            if (!_rooms.TryGetValue(roomUid, out var room)) { slotIndex = -1; return false; }
            return room.AddUnit(unitUid, out slotIndex);
        }
    }

    public bool RemoveUnit(long roomUid, long unitUid)
    {
        lock (_gate) return _rooms.TryGetValue(roomUid, out var room) && room.RemoveUnit(unitUid);
    }

    public BattleFieldRoom? FindJoinable(uint battleFieldId, int requiredSlots = 1)
    {
        if (requiredSlots <= 0) requiredSlots = 1;
        lock (_gate)
        {
            return _rooms.Values
                .Where(r => r.BattleFieldId == battleFieldId && r.State == RoomState.Wait)
                .Where(r => r.MaxSlot - r.JoinSlotCount >= requiredSlots)
                .OrderBy(r => r.JoinSlotCount)
                .ThenBy(r => r.RoomUid)
                .FirstOrDefault();
        }
    }

    public IReadOnlyList<RoomSimpleInfo> Snapshot()
    {
        lock (_gate) return _rooms.Values.Select(r => r.GetSimpleInfo()).ToArray();
    }

    public void Tick(float elapsedSeconds)
    {
        if (elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        lock (_gate)
            foreach (var room in _rooms.Values)
                room.Tick(elapsedSeconds);
    }
}
