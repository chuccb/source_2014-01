namespace KncWX2Server.Runtime.Center;

/// <summary>CenterServer RoomUserManager counterpart. Game and observer slots are kept as separate lists.</summary>
public sealed class RoomUserManager
{
    private readonly List<RoomSlot> _gameSlots=new();
    private readonly List<RoomSlot> _observerSlots=new();
    private readonly Dictionary<long,RoomUser> _gameUsers=new();
    private readonly Dictionary<long,RoomUser> _observerUsers=new();
    public int GameSlotCount=>_gameSlots.Count;
    public int ObserverSlotCount=>_observerSlots.Count;
    public IReadOnlyList<RoomSlot> GameSlots=>_gameSlots;
    public IReadOnlyList<RoomSlot> ObserverSlots=>_observerSlots;
    public int MemberCount=>_gameUsers.Count;
    public void Init(int gameSlots,int observerSlots=3){if(gameSlots<0||observerSlots<0)throw new ArgumentOutOfRangeException();_gameSlots.Clear();_observerSlots.Clear();_gameUsers.Clear();_observerUsers.Clear();for(var i=0;i<gameSlots;i++)_gameSlots.Add(new RoomSlot(i));for(var i=0;i<observerSlots;i++)_observerSlots.Add(new RoomSlot(i));}
    public RoomSlot? GetSlot(int slotId,bool observer=false){var slots=observer?_observerSlots:_gameSlots;return slotId>=0&&slotId<slots.Count?slots[slotId]:null;}
    public RoomUser? GetUser(long unitUid,bool observer=false){var users=observer?_observerUsers:_gameUsers;return users.GetValueOrDefault(unitUid);}
    public bool AddUser(RoomUser user,bool observer=false){ArgumentNullException.ThrowIfNull(user);var users=observer?_observerUsers:_gameUsers;return users.TryAdd(user.UserUid,user);}
    public bool DeleteUser(long unitUid,bool observer=false){var users=observer?_observerUsers:_gameUsers;users.Remove(unitUid);return true;}
    public int OpenedSlotCount(bool observer=false)=>(observer?_observerSlots:_gameSlots).Count(s=>s.IsOpened);
    public int OccupiedSlotCount(bool observer=false)=>(observer?_observerSlots:_gameSlots).Count(s=>s.IsOccupied);
    public int ReadyCount(bool observer=false)=>(observer?_observerSlots:_gameSlots).Count(s=>s.User?.IsReady==true);
    public int PlayingCount(bool observer=false)=>(observer?_observerSlots:_gameSlots).Count(s=>s.User?.StateMachine.State==RoomUserState.Play);
    public RoomUser? GetHost(bool observer=false)=>(observer?_observerSlots:_gameSlots).Select(s=>s.User).FirstOrDefault(u=>u?.IsHost==true);
    public bool EnterRoom(RoomUser user,bool considerTeam=true,bool observer=false){var slots=observer?_observerSlots:_gameSlots;var slot=slots.FirstOrDefault(s=>s.IsOpened&&!s.IsOccupied);if(slot is null||!AddUser(user,observer))return false;if(!slot.Enter(user)){DeleteUser(user.UserUid,observer);return false;}if(GetHost(observer) is null)user.SetHost(true);return true;}
    public bool LeaveRoom(long unitUid,bool observer=false){var user=GetUser(unitUid,observer);if(user is null)return true;var slots=observer?_observerSlots:_gameSlots;slots.FirstOrDefault(s=>ReferenceEquals(s.User,user))?.Leave();DeleteUser(unitUid,observer);if(user.IsHost)slots.Select(s=>s.User).FirstOrDefault(u=>u is not null)?.SetHost(true);return true;}
    public bool SetReady(long unitUid,bool ready,bool observer=false){var user=GetUser(unitUid,observer);return user is not null&&user.SetReady(ready);}
    public bool SetPitIn(long unitUid,bool value,bool observer=false){var user=GetUser(unitUid,observer);if(user is null)return false;user.SetPitIn(value);return true;}
    public bool SetTrade(long unitUid,bool value,bool observer=false){var user=GetUser(unitUid,observer);if(user is null)return false;user.SetTrade(value);return true;}
    public bool SetLoadingProgress(long unitUid,int value,bool observer=false){var user=GetUser(unitUid,observer);if(user is null)return false;user.SetLoadingProgress(value);return true;}
    public bool SetStageLoaded(long unitUid,bool value,bool observer=false){var user=GetUser(unitUid,observer);if(user is null)return false;user.SetStageLoaded(value);return true;}
    public IReadOnlyList<RoomSlotInfo> GetRoomSlotInfo(bool observer=false)=>(observer?_observerSlots:_gameSlots).Select(s=>s.GetRoomSlotInfo()).ToArray();
    public void Reset(bool observer=false){var slots=observer?_observerSlots:_gameSlots;foreach(var slot in slots)slot.ResetSlot();(observer?_observerUsers:_gameUsers).Clear();}
}
