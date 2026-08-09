namespace KncWX2Server.Runtime.Center;

public sealed class CenterRoomManager
{
    private readonly object _gate=new();
    private readonly Dictionary<long,CenterRoom> _rooms=new();
    public int Count{get{lock(_gate)return _rooms.Count;}}
    public CenterRoom Create(long roomUid,int roomType,int slotCount)
    {
        lock(_gate){if(_rooms.ContainsKey(roomUid))throw new InvalidOperationException($"Room {roomUid} already exists.");var room=new CenterRoom(roomUid,roomType,slotCount);_rooms.Add(roomUid,room);return room;}
    }
    public bool TryGet(long roomUid,out CenterRoom? room){lock(_gate)return _rooms.TryGetValue(roomUid,out room);}
    public bool Remove(long roomUid){lock(_gate)return _rooms.Remove(roomUid);}
    public CenterRoom[] Snapshot(){lock(_gate)return _rooms.Values.ToArray();}
    public CenterRoom? FindByUnitUid(long unitUid){lock(_gate)return _rooms.Values.FirstOrDefault(r=>r.FindByUnitUid(unitUid) is not null);}
}
