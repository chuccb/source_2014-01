namespace KncWX2Server.Runtime.BattleField;

public sealed class BattleFieldRoomManager
{
    private readonly object _sync=new();
    private readonly Dictionary<long,BattleFieldRoom> _rooms=new();
    private long _nextRoomUid=1;
    public int Count { get { lock(_sync)return _rooms.Count; } }
    public bool TryGet(long roomUid,out BattleFieldRoom? room){lock(_sync)return _rooms.TryGetValue(roomUid,out room);}
    public BattleFieldRoom Create(string name,int maxSlots,uint battleFieldId=uint.MaxValue){lock(_sync){var uid=_nextRoomUid++;var room=new BattleFieldRoom(uid,name,maxSlots,battleFieldId);_rooms.Add(uid,room);return room;}}
    public bool Remove(long roomUid){lock(_sync)return _rooms.Remove(roomUid);}
    public bool AddUnit(long roomUid,long unitUid,out int slot){lock(_sync){if(!_rooms.TryGetValue(roomUid,out var room)){slot=-1;return false;}return room.AddUnit(unitUid,out slot);}}
    public bool RemoveUnit(long roomUid,long unitUid){lock(_sync){return _rooms.TryGetValue(roomUid,out var room)&&room.RemoveUnit(unitUid);}}
    public IReadOnlyList<RoomSimpleInfo> Snapshot(){lock(_sync)return _rooms.Values.Select(x=>x.GetSimpleInfo()).ToArray();}
    public BattleFieldRoom? FindJoinable(uint battleFieldId,int preferredSlots=1){lock(_sync)return _rooms.Values.FirstOrDefault(x=>x.BattleFieldId==battleFieldId&&x.State==RoomState.Wait&&x.MaxSlot-x.JoinSlotCount>=preferredSlots);}
    public void Tick(float elapsed){lock(_sync)foreach(var room in _rooms.Values)room.Tick(elapsed);}
}
