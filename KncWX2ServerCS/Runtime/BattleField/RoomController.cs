namespace KncWX2Server.Runtime.BattleField;

public sealed class RoomController
{
    private readonly BattleFieldRoomManager _rooms;
    private readonly Dictionary<long, RoomStateMachine> _states = [];
    private readonly object _gate = new();

    public RoomController(BattleFieldRoomManager rooms) => _rooms = rooms;

    public BattleFieldRoom Create(string name, int maxSlots, uint battleFieldId = uint.MaxValue)
    {
        var room = _rooms.Create(name, maxSlots, battleFieldId);
        lock (_gate)
        {
            var state = new RoomStateMachine();
            state.Force(room.State);
            _states[room.RoomUid] = state;
        }

        return room;
    }

    public bool Transition(long roomUid, RoomState next)
    {
        lock (_gate)
        {
            if (!_states.TryGetValue(roomUid, out var state) ||
                !_rooms.TryGet(roomUid, out var room) ||
                room is null)
            {
                return false;
            }

            if (!state.TryTransition(next))
            {
                return false;
            }

            room.SetState(next);
            return true;
        }
    }

    public bool Join(long roomUid, long unitUid, out int slotIndex) => _rooms.AddUnit(roomUid, unitUid, out slotIndex);
    public bool Leave(long roomUid, long unitUid) => _rooms.RemoveUnit(roomUid, unitUid);
    public bool Ready(long roomUid, long unitUid, bool ready) => WithRoom(roomUid, room => room.SetReady(unitUid, ready));
    public bool ChangeTeam(long roomUid, long unitUid, TeamNum team) => WithRoom(roomUid, room => room.SetTeam(unitUid, team));
    public bool MakeHost(long roomUid, long unitUid) => WithRoom(roomUid, room => room.SetHost(unitUid));

    public bool SetPing(long roomUid, long unitUid, uint ping) =>
        WithRoom(roomUid, room =>
        {
            var slot = room.FindSlot(unitUid);
            if (slot is null)
            {
                return false;
            }

            slot.SetPing(ping);
            return true;
        });

    public bool Destroy(long roomUid)
    {
        lock (_gate)
        {
            if (_rooms.TryGet(roomUid, out var room) && room is not null)
            {
                room.OnCloseRoom();
            }

            _states.Remove(roomUid);
            return _rooms.Remove(roomUid);
        }
    }

    private bool WithRoom(long roomUid, Func<BattleFieldRoom, bool> action) =>
        _rooms.TryGet(roomUid, out var room) && room is not null && action(room);
}
