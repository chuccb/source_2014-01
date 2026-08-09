namespace KncWX2Server.Runtime.Center;

/// <summary>CenterServer counterpart of KRoomUserManager. Game and observer lists remain separate.</summary>
public sealed class RoomUserManager
{
    private readonly List<RoomSlot> _gameSlots=[];
    private readonly List<RoomSlot> _observerSlots=[];
    private readonly Dictionary<long,RoomUser> _gameUsers=[];
    private readonly Dictionary<long,RoomUser> _observerUsers=[];
    public int GameSlotCount=>_gameSlots.Count;
    public int ObserverSlotCount=>_observerSlots.Count;
    public IReadOnlyList<RoomSlot> GameSlots=>_gameSlots;
    public IReadOnlyList<RoomSlot> ObserverSlots=>_observerSlots;
    public int MemberCount=>_gameUsers.Count;
    public void Init(int gameSlots,int observerSlots=RoomUser.DefaultObserverSlotCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gameSlots);ArgumentOutOfRangeException.ThrowIfNegative(observerSlots);
        _gameSlots.Clear();_observerSlots.Clear();_gameUsers.Clear();_observerUsers.Clear();
        for(var i=0;i<gameSlots;i++)_gameSlots.Add(new RoomSlot(i));
        for(var i=0;i<observerSlots;i++)_observerSlots.Add(new RoomSlot(i));
    }
    private List<RoomSlot> Slots(bool observer)=>observer?_observerSlots:_gameSlots;
    private Dictionary<long,RoomUser> Users(bool observer)=>observer?_observerUsers:_gameUsers;
    public RoomSlot? GetSlot(int slotId,bool observer=false){var slots=Slots(observer);return slotId>=0&&slotId<slots.Count?slots[slotId]:null;}
    public RoomUser? GetUser(long cid,bool observer=false)=>Users(observer).GetValueOrDefault(cid);
    public bool AddUser(RoomUser user,bool observer=false){ArgumentNullException.ThrowIfNull(user);return Users(observer).TryAdd(user.Cid,user);}
    public bool DeleteUser(long cid,bool observer=false){Users(observer).Remove(cid);return true;}
    public bool DeleteUserByGsUid(long gsUid,List<(long UnitUid,long PartyUid)> removed,bool observer=false){ArgumentNullException.ThrowIfNull(removed);var users=Users(observer);var matches=users.Values.Where(u=>u.GSUid==gsUid).ToArray();foreach(var u in matches){removed.Add((u.Cid,u.PartyUid));users.Remove(u.Cid);}return matches.Length>0;}
    public int TotalSlotCount(bool observer=false)=>Slots(observer).Count;
    public int OpenedSlotCount(bool observer=false)=>Slots(observer).Count(s=>s.IsOpened);
    public int OccupiedSlotCount(bool observer=false)=>Slots(observer).Count(s=>s.IsOccupied);
    public int ReadyCount(bool observer=false)=>Users(observer).Values.Count(u=>u.IsReady);
    public int PlayingCount(bool observer=false)=>Users(observer).Values.Count(u=>u.IsPlaying());
    public int ResultCount(bool observer=false)=>Users(observer).Values.Count(u=>u.State==RoomUserState.Result);
    public int LiveMemberCount(bool observer=false){var count=Users(observer).Values.Count(u=>!u.IsDie);return Math.Max(1,count);}
    public int TeamPlayingCount(int team,bool observer=false)=>Users(observer).Values.Count(u=>u.Team==team&&u.IsPlaying());
    public RoomUser? GetHost(bool observer=false)=>Users(observer).Values.FirstOrDefault(u=>u.IsHost);
    public bool EnterRoom(RoomUser user,bool considerTeam=true,bool observer=false)
    {
        ArgumentNullException.ThrowIfNull(user);var slots=Slots(observer);
        var slot=slots.FirstOrDefault(s=>s.IsOpened&&!s.IsOccupied && (!considerTeam||s.Team==user.Team)) ?? slots.FirstOrDefault(s=>s.IsOpened&&!s.IsOccupied);
        if(slot is null||!AddUser(user,observer))return false;
        if(!slot.Enter(user)){DeleteUser(user.Cid,observer);return false;}
        if(!observer&&!_gameUsers.Values.Any(u=>u.IsHost))user.SetHost(true);
        return true;
    }
    public bool LeaveRoom(long cid,bool observer=false)
    {
        var user=GetUser(cid,observer);if(user is null)return true;var wasHost=user.IsHost;
        GetSlot(user.SlotId,observer)?.Leave();DeleteUser(cid,observer);
        if(wasHost&&!observer){var replacement=_gameUsers.Values.FirstOrDefault();if(replacement is not null)replacement.SetHost(true);}
        return true;
    }
    public bool LeaveGame(long cid){var user=GetUser(cid);if(user is null)return false;user.EndGame();if(user.IsHost){var replacement=_gameUsers.Values.FirstOrDefault(u=>u.Cid!=cid);if(replacement is not null){replacement.SetHost(true);user.SetHost(false);}}return true;}
    public bool SetReady(long cid,bool ready,bool observer=false)=>GetUser(cid,observer)?.SetReady(ready)==true;
    public bool SetPitIn(long cid,bool value)=>GetUser(cid)?.Let(u=>{u.SetPitIn(value);return true;})==true;
    public bool SetTrade(long cid,bool value)=>GetUser(cid)?.Let(u=>{u.SetTrade(value);return true;})==true;
    public bool SetLoadingProgress(long cid,int value)=>GetUser(cid)?.Let(u=>{u.SetLoadingProgress(value);return true;})==true;
    public bool SetStageLoaded(long cid,bool value)=>GetUser(cid)?.Let(u=>{u.SetStageLoaded(value);return true;})==true;
    public bool ChangeTeam(long cid,int destinationTeam)
    {
        var user=GetUser(cid);if(user is null)return false;var from=GetSlot(user.SlotId);if(from is null||from.Team==destinationTeam)return from is not null;
        var target=_gameSlots.FirstOrDefault(s=>s.IsOpened&&!s.IsOccupied&&s.Team==destinationTeam);if(target is null)return false;
        from.Leave();return target.Enter(user);
    }
    public bool IsReady(long cid,out bool ready){var u=GetUser(cid);ready=u?.IsReady==true;return u is not null;}
    public bool IsInTrade(long cid,out bool trade){var u=GetUser(cid);trade=u?.IsInTrade==true;return u is not null;}
    public bool GetTeam(long cid,out int team){var u=GetUser(cid);team=u?.Team??0;return u is not null;}
    public bool IncreaseNumKill(long cid){var u=GetUser(cid);if(u is null)return false;u.IncreaseKill();return true;}
    public bool IncreaseNumMDKill(long cid){var u=GetUser(cid);if(u is null)return false;u.IncreaseMDKill();return true;}
    public bool IncreaseNumDie(long cid){var u=GetUser(cid);if(u is null)return false;u.IncreaseDie();return true;}
    public bool SetDie(long cid,bool value){var u=GetUser(cid);if(u is null)return false;u.SetDie(value);return true;}
    public bool SetHP(long cid,float value){var u=GetUser(cid);if(u is null)return false;u.SetHP(value);return true;}
    public bool SetStageId(long cid,int value){var u=GetUser(cid);if(u is null)return false;u.SetStage(value);return true;}
    public bool SetRebirthPos(long cid,int value){var u=GetUser(cid);if(u is null)return false;u.SetRebirthPos(value);return true;}
    public void StartGame(){foreach(var u in _gameUsers.Values.Concat(_observerUsers.Values))u.StartGame();}
    public void StartPlay(){foreach(var u in _gameUsers.Values.Concat(_observerUsers.Values))u.StartPlay();}
    public void StartResult(){foreach(var u in _gameUsers.Values.Concat(_observerUsers.Values))u.StartResult();}
    public void EndPlay(){foreach(var u in _gameUsers.Values)u.StartResult();}
    public void EndGame(){foreach(var u in _gameUsers.Values.Concat(_observerUsers.Values))u.EndGame();}
    public bool IsAllPlayerReady()=>_gameUsers.Count>0&&_gameUsers.Values.All(u=>u.IsReady);
    public bool IsAllPlayerFinishLoading()=>_gameUsers.Count>0&&_gameUsers.Values.All(u=>u.State!=RoomUserState.Init);
    public bool IsAllPlayerStageLoaded()=>_gameUsers.Count>0&&_gameUsers.Values.All(u=>u.IsStageLoaded);
    public bool IsAllPlayerAlive()=>_gameUsers.Count>0&&_gameUsers.Values.All(u=>!u.IsDie);
    public bool IsAllPlayerDie()=>_gameUsers.Count>0&&_gameUsers.Values.All(u=>u.IsDie);
    public bool IsAllPlayerSuccessResult()=>_gameUsers.Count>0&&_gameUsers.Values.All(u=>u.State==RoomUserState.Result);
    public IReadOnlyList<RoomSlotInfo> GetRoomSlotInfo(bool observer=false)=>Slots(observer).Select(s=>s.Snapshot()).ToArray();
    public void Reset(bool observer=false){foreach(var slot in Slots(observer))slot.ResetSlot();Users(observer).Clear();}
}

file static class RoomUserExtensions
{
    public static TResult Let<T,TResult>(this T value,Func<T,TResult> func)=>func(value);
}
