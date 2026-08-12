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

    public int BattleFieldCount => _roomsByBattleField.Count;
    public int RoomCount => _roomsByBattleField.Values.Sum(static rooms => rooms.Count);

    public bool AddRoom(BattleFieldRoom room)
    {
        ArgumentNullException.ThrowIfNull(room);

        var rooms = _roomsByBattleField.GetOrAdd(room.BattleFieldId);
        return rooms.TryAdd(room.RoomUid, room);
    }

    public bool RemoveRoom(uint battleFieldId, long roomUid)
    {
        if (!_roomsByBattleField.TryGetValue(battleFieldId, out var rooms))
            return false;

        var removed = rooms.Remove(roomUid);
        if (rooms.Count == 0)
            _roomsByBattleField.Remove(battleFieldId);

        return removed;
    }

    public bool TryGetRoom(uint battleFieldId, long roomUid, out BattleFieldRoom? room)
    {
        if (_roomsByBattleField.TryGetValue(battleFieldId, out var rooms))
            return rooms.TryGetValue(roomUid, out room);

        room = null;
        return false;
    }

    public IReadOnlyList<BattleFieldRoom> GetRooms(uint battleFieldId) =>
        _roomsByBattleField.TryGetValue(battleFieldId, out var rooms)
            ? rooms.Values.OrderBy(static room => room.RoomUid).ToArray()
            : [];

    public bool TrySelectRoom(
        BattleFieldRoomJoinRequest request,
        Func<BattleFieldRoom, int>? partyMemberCount = null,
        Func<BattleFieldRoom, bool>? unitAlreadyJoined = null,
        Func<IReadOnlyList<BattleFieldRoom>, BattleFieldRoom?>? randomSelector = null,
        out BattleFieldRoom? selectedRoom)
    {
        selectedRoom = null;
        if (request.RequiredSlots <= 0 || !_roomsByBattleField.TryGetValue(request.BattleFieldId, out var rooms))
            return false;

        // Native order: largest matching-party presence first.
        if (request.PartyUid != 0 && partyMemberCount is not null)
        {
            var partyRoom = rooms.Values
                .Where(room => IsJoinable(room, request.RequiredSlots, unitAlreadyJoined?.Invoke(room) == true))
                .Select(room => new
                {
                    Room = room,
                    PartyMembers = Math.Max(0, partyMemberCount(room) - (unitAlreadyJoined?.Invoke(room) == true ? 1 : 0)),
                })
                .Where(static candidate => candidate.PartyMembers > 0)
                .OrderByDescending(static candidate => candidate.PartyMembers)
                .ThenBy(static candidate => candidate.Room.RoomUid)
                .Select(static candidate => candidate.Room)
                .FirstOrDefault();

            if (partyRoom is not null)
            {
                selectedRoom = partyRoom;
                return true;
            }
        }

        // Native order: try the target room before random selection.
        if (request.TargetRoomUid != 0 && rooms.TryGetValue(request.TargetRoomUid, out var targetRoom) &&
            IsJoinable(targetRoom, request.RequiredSlots, unitAlreadyJoined?.Invoke(targetRoom) == true))
        {
            selectedRoom = targetRoom;
            return true;
        }

        var candidates = rooms.Values
            .Where(room => IsJoinable(room, request.RequiredSlots, unitAlreadyJoined?.Invoke(room) == true))
            .OrderBy(static room => room.RoomUid)
            .ToArray();

        if (candidates.Length == 0)
            return true;

        selectedRoom = randomSelector?.Invoke(candidates) ?? candidates[Random.Shared.Next(candidates.Length)];
        return true;
    }

    private static bool IsJoinable(BattleFieldRoom room, int requiredSlots, bool unitAlreadyJoined)
    {
        if (room.State != RoomState.Wait)
            return false;

        var freeSlots = room.MaxSlot - room.JoinSlotCount;
        return freeSlots >= (unitAlreadyJoined ? requiredSlots - 1 : requiredSlots);
    }

    public void Clear() => _roomsByBattleField.Clear();
}

file static class BattleFieldRoomCatalogExtensions
{
    public static Dictionary<TKey, TValue> GetOrAdd<TKey, TValue>(
        this Dictionary<TKey, Dictionary<long, TValue>> dictionary,
        TKey key)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var rooms))
            return rooms;

        rooms = [];
        dictionary.Add(key, rooms);
        return rooms;
    }
}
