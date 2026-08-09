namespace KncWX2Server.Runtime.Center;

public sealed class CenterRoom
{
    private readonly RoomSlot[] _slots;
    public long RoomUid { get; }
    public int RoomType { get; }
    public RoomStateMachine StateMachine { get; }=new();
    public IReadOnlyList<RoomSlot> Slots=>_slots;
    public int OccupiedCount=>_slots.Count(static s=>s.IsOccupied);
    public bool IsEmpty=>OccupiedCount==0;

    public CenterRoom(long roomUid,int roomType,int slotCount)
    {
        if(slotCount<=0)throw new ArgumentOutOfRangeException(nameof(slotCount));
        RoomUid=roomUid;RoomType=roomType;
        _slots=Enumerable.Range(0,slotCount).Select(static i=>new RoomSlot(i)).ToArray();
    }

    public RoomSlot? FindByUnitUid(long unitUid)=>_slots.FirstOrDefault(s=>s.User?.UnitUid==unitUid);
    public RoomSlot? FindByUserUid(long userUid)=>_slots.FirstOrDefault(s=>s.User?.UserUid==userUid);
    public RoomSlot? FindEmptyOpenedSlot()=>_slots.FirstOrDefault(static s=>s.IsOpened&&!s.IsOccupied);
    public bool Join(RoomUser user){if(user is null||FindByUnitUid(user.UnitUid) is not null)return false;var slot=FindEmptyOpenedSlot();return slot is not null&&slot.Enter(user)&&EnsureHost();}
    public bool LeaveByUnitUid(long unitUid){var slot=FindByUnitUid(unitUid);if(slot is null)return false;var wasHost=slot.User?.IsHost==true;if(!slot.Leave())return false;if(wasHost)EnsureHost();return true;}
    public bool SetReady(long unitUid,bool ready){var user=FindByUnitUid(unitUid)?.User;return user is not null&&user.SetReady(ready);}
    public bool SetTrade(long unitUid,bool value){var user=FindByUnitUid(unitUid)?.User;if(user is null)return false;user.SetTrade(value);return true;}
    public bool ChangeTeam(long unitUid,int team){var user=FindByUnitUid(unitUid)?.User;if(user is null)return false;user.SetTeam(team);return true;}
    public RoomSlotInfo[] Snapshot()=>_slots.Select(static s=>s.GetRoomSlotInfo()).ToArray();
    private bool EnsureHost(){if(_slots.Any(static s=>s.User?.IsHost==true))return true;var user=_slots.FirstOrDefault(static s=>s.IsOccupied)?.User;if(user is null)return true;return user.SetHost(true);}
}
