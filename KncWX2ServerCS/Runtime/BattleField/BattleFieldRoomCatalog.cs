namespace KncWX2Server.Runtime.BattleField;

/// <summary>Input used by the native GetRoomUIDForJoinBattleField selection flow.</summary>
public readonly record struct BattleFieldRoomJoinRequest(
    uint BattleFieldId,
    long UnitUid,
    long TargetRoomUid,
    long PartyUid,
    int RequiredSlots = 1);

/// <summary>
/// Managed counterpart of KBattleFieldList/KBattleFieldListManager.
/// X2Data room metadata and party membership remain injectable so the selection rules
/// can be verified without fabricating native packet/data-table state.
/// </summary>
public sealed class BattleFieldRoomCatalog
{
    private readonly Dictionary<uint, Dictionary<long, BattleFieldRoom>> _roomsByBattleField = [];
    private readonly Dictionary<uint, List<long>> _roomOrder = [];

    public int BattleFieldCount => _roomsByBattleField.Count;
    public int RoomCount => _roomsByBattleField.Values.Sum(static rooms => rooms.Count);

    public bool AddRoom(BattleFieldRoom room)
    {
        ArgumentNullException.ThrowIfNull(room);

        if (!_roomsByBattleField.TryGetValue(room.BattleFieldId, out var rooms))
        {
            rooms = [];
            _roomsByBattleField.Add(room.BattleFieldId, rooms);
            _roomOrder.Add(room.BattleFieldId, []);
        }

        if (!rooms.TryAdd(room.RoomUid, room))
            return false;

        _roomOrder[room.BattleFieldId].Add(room.RoomUid);
        return true;
    }

    public bool RemoveRoom(uint battleFieldId, long roomUid)
    {
        if (!_roomsByBattleField.TryGetValue(battleFieldId, out var rooms))
            return false;

        if (!rooms.Remove(roomUid))
            return false;

        _roomOrder[battleFieldId].Remove(roomUid);
        if (rooms.Count == 0)
        {
            _roomsByBattleField.Remove(battleFieldId);
            _roomOrder.Remove(battleFieldId);
        }

        return true;
    }

    public bool TryGetRoom(uint battleFieldId, long roomUid, out BattleFieldRoom? room)
    {
        if (_roomsByBattleField.TryGetValue(battleFieldId, out var rooms))
            return rooms.TryGetValue(roomUid, out room);

        room = null;
        return false;
    }

    public IReadOnlyList<BattleFieldRoom> GetRooms(uint battleFieldId)
    {
        if (!_roomsByBattleField.TryGetValue(battleFieldId, out var rooms) || !_roomOrder.TryGetValue(battleFieldId, out var order))
            return [];

        return order
            .Where(rooms.ContainsKey)
            .Select(uid => rooms[uid])
            .ToArray();
    }

    public bool TrySelectRoom(
        BattleFieldRoomJoinRequest request,
        out BattleFieldRoom? selectedRoom,
        Func<BattleFieldRoom, int>? partyMemberCount = null,
        Func<BattleFieldRoom, bool>? unitAlreadyJoined = null,
        Func<BattleFieldRoom, int>? reservedUserCount = null,
        Func<IReadOnlyList<BattleFieldRoom>, BattleFieldRoom?>? randomSelector = null)
    {
        selectedRoom = null;
        if (request.RequiredSlots <= 0 ||
            !_roomsByBattleField.TryGetValue(request.BattleFieldId, out var rooms) ||
            !_roomOrder.TryGetValue(request.BattleFieldId, out var order))
        {
            return false;
        }

        var orderedRooms = order.Where(rooms.ContainsKey).Select(uid => rooms[uid]).ToArray();

        // Native order: largest matching-party presence first. Ties retain m_vecList order.
        if (request.PartyUid != 0 && partyMemberCount is not null)
        {
            var bestCount = 0;
            foreach (var room in orderedRooms)
            {
                var alreadyJoined = unitAlreadyJoined?.Invoke(room) == true;
                var partyMembers = Math.Max(0, partyMemberCount(room) - (alreadyJoined ? 1 : 0));
                var availableSlots = room.MaxSlot - (alreadyJoined ? room.JoinSlotCount - 1 : room.JoinSlotCount);

                if (availableSlots <= 0 || partyMembers <= bestCount)
                    continue;

                bestCount = partyMembers;
                selectedRoom = room;
            }

            if (selectedRoom is not null)
                return true;
        }

        // Native order: try the target room before random selection.
        if (request.TargetRoomUid != 0 && rooms.TryGetValue(request.TargetRoomUid, out var targetRoom) &&
            HasReservedAwareCapacity(targetRoom, request.RequiredSlots, unitAlreadyJoined?.Invoke(targetRoom) == true, reservedUserCount))
        {
            selectedRoom = targetRoom;
            return true;
        }

        // Native fallback: uniformly select among rooms with reserved-aware capacity.
        var candidates = orderedRooms
            .Where(room => HasReservedAwareCapacity(room, request.RequiredSlots, unitAlreadyJoined?.Invoke(room) == true, reservedUserCount))
            .ToArray();

        if (candidates.Length == 0)
            return true;

        selectedRoom = randomSelector?.Invoke(candidates) ?? candidates[Random.Shared.Next(candidates.Length)];
        return true;
    }

    private static bool HasReservedAwareCapacity(
        BattleFieldRoom room,
        int requiredSlots,
        bool unitAlreadyJoined,
        Func<BattleFieldRoom, int>? reservedUserCount)
    {
        if (room.State != RoomState.Wait)
            return false;

        var reserved = Math.Max(0, reservedUserCount?.Invoke(room) ?? 0);
        var occupied = room.JoinSlotCount + reserved;
        if (unitAlreadyJoined)
            occupied--;

        return room.MaxSlot - occupied >= requiredSlots;
    }

    public void Clear()
    {
        _roomsByBattleField.Clear();
        _roomOrder.Clear();
    }
}
