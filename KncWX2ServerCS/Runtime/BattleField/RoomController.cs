namespace KncWX2Server.Runtime.BattleField;

public sealed class RoomController
{
    private readonly BattleFieldRoomManager _rooms;
    private readonly Dictionary<long,RoomStateMachine> _states=new();
    private readonly object _gate=new();
    public RoomController(BattleFieldRoomManager rooms)=>_rooms=rooms;
    public BattleFieldRoom Create(string name,int maxSlots,uint battleFieldId=uint.MaxValue){var room=_rooms.Create(name,maxSlots,battleFieldId);lock(_gate)_states[room.RoomUid]=new RoomStateMachine();return room;}
    public bool Transition(long roomUid,RoomState next){lock(_gate){if(!_states.TryGetValue(roomUid,out var state)||!_rooms.TryGet(roomUid,out var room)||room is null)return false;if(!state.TryTransition(next))return false;room.SetState(next);return true;}}
    public bool Join(long roomUid,long unitUid,out int slotIndex)=>_rooms.AddUnit(roomUid,unitUid,out slotIndex);
    public bool Leave(long roomUid,long unitUid)=>_rooms.RemoveUnit(roomUid,unitUid);
    public bool Ready(long roomUid,long unitUid,bool ready)=>WithRoom(roomUid,r=>r.SetReady(unitUid,ready));
    public bool ChangeTeam(long roomUid,long unitUid,TeamNum team)=>WithRoom(roomUid,r=>r.SetTeam(unitUid,team));
    public bool MakeHost(long roomUid,long unitUid)=>WithRoom(roomUid,r=>r.SetHost(unitUid));
    public bool SetPing(long roomUid,long unitUid,uint ping)=>WithRoom(roomUid,r=>{var s=r.FindSlot(unitUid);if(s is null)return false;s.SetPing(ping);return true;});
    public bool Destroy(long roomUid){lock(_gate)_states.Remove(roomUid);return _rooms.Remove(roomUid);}
    private bool WithRoom(long roomUid,Func<BattleFieldRoom,bool> action){if(!_rooms.TryGet(roomUid,out var room)||room is null)return false;return action(room);}
}
