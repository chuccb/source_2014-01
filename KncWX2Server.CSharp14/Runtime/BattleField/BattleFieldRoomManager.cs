namespace KncWX2Server.Runtime.BattleField;

/// <summary>
/// Owns battle-field rooms and serializes mutations to the room collection.
/// This preserves the locking semantics of the existing KncWX2ServerCS implementation
/// while using the dedicated .NET 10/C# 14 lock primitive.
/// </summary>
public sealed class BattleFieldRoomManager
{
    private readonly Lock _gate = new();
    private readonly Dictionary<long, BattleFieldRoom> _rooms = [];
    private long _nextRoomUid = 1;

    public int Count
    {
        get
        {
            lock (_gate)
                return _rooms.Count;
        }
    }

    public bool TryGet(long roomUid, out BattleFieldRoom? room)
    {
        lock (_gate)
            return _rooms.TryGetValue(roomUid, out room);
    }

    public BattleFieldRoom Create(
        string name,
        int maxSlots,
        uint battleFieldId = uint.MaxValue,
        bool isPublic = true)
    {
        lock (_gate)
        {
            var roomUid = _nextRoomUid++;
            var room = new BattleFieldRoom(roomUid, name, maxSlots, battleFieldId, isPublic);
            _rooms.Add(roomUid, room);
            return room;
        }
    }

    public bool Remove(long roomUid)
    {
        lock (_gate)
            return _rooms.Remove(roomUid);
    }

    public bool AddUnit(long roomUid, long unitUid, out int slot)
    {
        lock (_gate)
        {
            if (!_rooms.TryGetValue(roomUid, out var room))
            {
                slot = -1;
                return false;
            }

            return room.AddUnit(unitUid, out slot);
        }
    }

    public bool RemoveUnit(long roomUid, long unitUid)
    {
        lock (_gate)
            return _rooms.TryGetValue(roomUid, out var room) && room.RemoveUnit(unitUid);
    }

    public IReadOnlyList<RoomSimpleInfo> Snapshot()
    {
        lock (_gate)
            return [.. _rooms.Values.Select(static room => room.GetSimpleInfo())];
    }

    public BattleFieldRoom? FindJoinable(uint battleFieldId, int preferredSlots = 1)
    {
        if (preferredSlots <= 0)
            throw new ArgumentOutOfRangeException(nameof(preferredSlots));

        lock (_gate)
        {
            return _rooms.Values.FirstOrDefault(room =>
                room.BattleFieldId == battleFieldId &&
                room.State == RoomState.Wait &&
                room.FreeSlotCount >= preferredSlots);
        }
    }

    public void Tick(float elapsedSeconds)
    {
        if (elapsedSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));

        lock (_gate)
        {
            foreach (var room in _rooms.Values)
                room.Tick(elapsedSeconds);
        }
    }
}
