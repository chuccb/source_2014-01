namespace KncWX2Server.Runtime.Center;

public enum RoomSlotState { Init=0, Closed=1, Assigned=2 }

public sealed record RoomSlotInfo(int Index,RoomSlotState State,int Team,bool Host,bool Ready,bool PitIn,bool Trade,long UnitUid);

public sealed class RoomSlot
{
    public int SlotId { get; }
    public int Team { get; private set; }
    public RoomSlotState State { get; private set; }=RoomSlotState.Init;
    public RoomUser? User { get; private set; }
    public bool IsOpened=>State is RoomSlotState.Init or RoomSlotState.Assigned;
    public bool IsOccupied=>State==RoomSlotState.Assigned&&User is not null;
    public RoomSlot(int slotId,int team=0){if(slotId<0)throw new ArgumentOutOfRangeException(nameof(slotId));SlotId=slotId;Team=team;}
    public void AssignTeam(int team)=>Team=team;
    public bool Enter(RoomUser user){ArgumentNullException.ThrowIfNull(user);if(!IsOpened||IsOccupied)return false;User=user;user.SetSlotId(SlotId);user.SetTeam(Team);State=RoomSlotState.Assigned;return true;}
    public bool Leave(){if(!IsOccupied)return true;User=null;State=RoomSlotState.Init;return true;}
    public bool Open(){if(State==RoomSlotState.Closed){State=RoomSlotState.Init;return true;}return State==RoomSlotState.Init;}
    public bool Close(){if(IsOccupied)return false;if(State==RoomSlotState.Init){State=RoomSlotState.Closed;return true;}return State==RoomSlotState.Closed;}
    public bool ToggleOpenClose()=>State switch{RoomSlotState.Init=>Close(),RoomSlotState.Closed=>Open(),RoomSlotState.Assigned=>false,_=>false};
    public void ResetSlot(){User=null;State=RoomSlotState.Init;}
    public RoomSlotInfo Snapshot()=>new(SlotId,State,Team,User?.IsHost==true,User?.IsReady==true,User?.IsPitIn==true,User?.IsInTrade==true,User?.UserUid??0);
}
