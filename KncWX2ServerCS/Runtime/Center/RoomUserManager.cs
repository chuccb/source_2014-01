namespace KncWX2Server.Runtime.Center;

public sealed class RoomUserManager
{
    public enum UserListType { Game=0, Observer=1 }
    private readonly List<RoomSlot> _gameSlots=new();
    private readonly List<RoomSlot> _observerSlots=new();
    private readonly Dictionary<long,RoomUser> _gameUsers=new();
    private readonly Dictionary<long,RoomUser> _observerUsers=new();
    private readonly object _gate=new();
    private readonly Dictionary<int,int> _teamNumKill=new();
    private int _gameMode;
    public IReadOnlyList<RoomSlot> GameSlots=>_gameSlots;
    public IReadOnlyList<RoomSlot> ObserverSlots=>_observerSlots;
    public void Init(int gameSlotCount,int observerSlotCount=3,int gameMode=0){if(gameSlotCount<0||observerSlotCount<0)throw new ArgumentOutOfRangeException();lock(_gate){ResetInternal(UserListType.Game);ResetInternal(UserListType.Observer);_gameMode=gameMode;for(var i=0;i<gameSlotCount;i++)_gameSlots.Add(new RoomSlot(i));for(var i=0;i<observerSlotCount;i++)_observerSlots.Add(new RoomSlot(i));AssignTeamInternal(_gameSlots);AssignTeamInternal(_observerSlots);}}
    public RoomSlot? GetSlot(int slotId,UserListType type=UserListType.Game){lock(_gate)return GetSlotUnsafe(slotId,type);}
    public RoomUser? GetUser(long unitUid,UserListType type=UserListType.Game){lock(_gate)return GetUserUnsafe(unitUid,type);}
    public int GetNumTotalSlot(UserListType type=UserListType.Game){lock(_gate)return Slots(type).Count;}
    public int GetNumOpenedSlot(UserListType type=UserListType.Game){lock(_gate)return Slots(type).Count(x=>x.IsOpened);}
    public int GetNumOccupiedSlot(UserListType type=UserListType.Game){lock(_gate)return Slots(type).Count(x=>x.IsOccupied);}
    public int GetNumMember(UserListType type=UserListType.Game){lock(_gate)return Users(type).Count;}
    public int GetNumPlaying(UserListType type=UserListType.Game){lock(_gate)return Users(type).Values.Count(x=>x.IsPlaying);}
    public int GetNumResultPlayer(UserListType type=UserListType.Game){lock(_gate)return Users(type).Values.Count(x=>x.StateMachine.State==RoomUserState.Result);}
    public int GetNumReadyPlayer(UserListType type=UserListType.Game){lock(_gate)return Users(type).Values.Count(x=>x.IsReady);}
    public int GetLiveMember(UserListType type=UserListType.Game){lock(_gate){var count=Users(type).Values.Count(x=>!x.IsDie);return count<=0?1:count;}}
    public int GetTeamNumPlaying(int team,UserListType type=UserListType.Game){lock(_gate)return Users(type).Values.Count(x=>x.Team==team&&x.IsPlaying);}
    public bool AddUser(RoomUser user,UserListType type=UserListType.Game){ArgumentNullException.ThrowIfNull(user);lock(_gate)return AddUserUnsafe(user,type);}
    public bool DeleteUser(long unitUid,UserListType type=UserListType.Game){lock(_gate)return DeleteUserUnsafe(unitUid,type);}
    public bool EnterRoom(RoomUser user,bool considerTeam=true,UserListType type=UserListType.Game){ArgumentNullException.ThrowIfNull(user);lock(_gate){var users=Users(type);if(users.ContainsKey(user.UnitUid))return false;var slots=Slots(type);RoomSlot? slot=null;if(considerTeam)slot=FindEmptyTeamSlotInternal(slots,ChooseTeamInternal(users));slot??=slots.FirstOrDefault(x=>x.IsOpened&&!x.IsOccupied);if(slot is null)return false;if(!AddUserUnsafe(user,type))return false;if(!slot.Enter(user)){users.Remove(user.UnitUid);return false;}if(type==UserListType.Game&&GetNumOccupiedSlotUnsafe(type)==1)user.SetHost(true);return true;}}
    public bool LeaveRoom(long unitUid,UserListType type=UserListType.Game)=>DeleteUser(unitUid,type);
    public void LeaveAllUnit(){lock(_gate){var gameIds=_gameUsers.Keys.ToArray();var observerIds=_observerUsers.Keys.ToArray();foreach(var id in gameIds)DeleteUserUnsafe(id,UserListType.Game);foreach(var id in observerIds)DeleteUserUnsafe(id,UserListType.Observer);}}
    public bool LeaveGame(long unitUid){lock(_gate){var type=UserListType.Game;var user=GetUserUnsafe(unitUid,type);if(user is null){type=UserListType.Observer;user=GetUserUnsafe(unitUid,type);}if(user is null)return false;var wasHost=user.IsHost;user.EndGame();if(type==UserListType.Game&&wasHost&&Users(type).Count>1){if(!AppointNewHostUnsafe(type,user.UnitUid))return false;user.SetHost(false);}return true;}}
    public bool ChangeTeam(long unitUid,int destinationTeam,UserListType type=UserListType.Game){lock(_gate){var user=GetUserUnsafe(unitUid,type);if(user is null)return false;var source=Slots(type).FirstOrDefault(x=>ReferenceEquals(x.User,user));if(source is null)return false;if(source.Team==destinationTeam)return true;var destination=Slots(type).FirstOrDefault(x=>x.IsOpened&&!x.IsOccupied&&x.Team==destinationTeam);if(destination is null)return false;if(!source.Leave())return false;return destination.Enter(user);}}
    public bool SetReady(long unitUid,bool ready,UserListType type=UserListType.Game){lock(_gate)return GetUserUnsafe(unitUid,type)?.SetReady(ready)==true;}
    public bool SetAllReady(bool ready){lock(_gate){foreach(var u in _gameUsers.Values)u.SetReady(ready);return true;}}
    public bool SetPitIn(long unitUid,bool value,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.SetPitIn(value);return true;}}
    public bool SetTrade(long unitUid,bool value,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.SetTrade(value);return true;}}
    public bool SetLoadingProgress(long unitUid,int value,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.SetLoadingProgress(value);return true;}}
    public bool SetStageLoaded(long unitUid,bool value,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.SetStageLoaded(value);return true;}}
    public void ResetStageLoaded(){lock(_gate)foreach(var u in _gameUsers.Values)u.SetStageLoaded(false);}
    public bool SetDie(long unitUid,bool value,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.SetDie(value);return true;}}
    public bool SetHP(long unitUid,float value,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.SetHP(value);return true;}}
    public bool IncreaseNumKill(long unitUid,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.IncreaseKill();return true;}}
    public bool IncreaseNumMDKill(long unitUid,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.IncreaseMDKill();return true;}}
    public bool IncreaseNumDie(long unitUid,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.IncreaseDie();return true;}}
    public bool IncreaseTeamNumKill(long unitUid){lock(_gate){var u=GetUserUnsafe(unitUid,UserListType.Game);if(u is null)return false;_teamNumKill[u.Team]=_teamNumKill.GetValueOrDefault(u.Team)+1;return true;}}
    public bool SetStageId(long unitUid,int value,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.SetStage(value);return true;}}
    public bool SetSubStageId(long unitUid,int value,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.SetSubStage(value);return true;}}
    public bool SetRebirthPos(long unitUid,int value,UserListType type=UserListType.Game){lock(_gate){var u=GetUserUnsafe(unitUid,type);if(u is null)return false;u.SetRebirthPos(value);return true;}}
    public void StartGame(UserListType type=UserListType.Game){lock(_gate)foreach(var u in Users(type).Values)if(u.IsReady)u.StartGame();}
    public void StartPlay(UserListType type=UserListType.Game){lock(_gate){foreach(var u in Users(type).Values)if(u.StateMachine.State==RoomUserState.Load)u.StartPlay();_teamNumKill.Clear();}}
    public void StartResult(UserListType type=UserListType.Game){lock(_gate)foreach(var u in Users(type).Values)if(u.StateMachine.State==RoomUserState.Play)u.EndPlay();}
    public void EndPlay(UserListType type=UserListType.Game){lock(_gate)foreach(var u in Users(type).Values)if(u.StateMachine.State==RoomUserState.Play)u.EndPlay();}
    public void EndGame(UserListType type=UserListType.Game){lock(_gate)foreach(var u in Users(type).Values)if(u.StateMachine.State==RoomUserState.Result)u.EndGame();}
    public bool IsAllPlayerReady(UserListType type=UserListType.Game){lock(_gate){foreach(var u in Users(type).Values)if(!u.IsReady)return false;return true;}}
    public bool IsAllPlayerFinishLoading(UserListType type=UserListType.Game){lock(_gate){foreach(var u in Users(type).Values)if(u.IsPlaying&&u.LoadingProgress>=0&&u.LoadingProgress<100)return false;return true;}}
    public bool IsAllPlayerStageLoaded(UserListType type=UserListType.Game){lock(_gate){foreach(var u in Users(type).Values)if(u.IsPlaying&&!u.IsStageLoaded)return false;return true;}}
    public bool IsAllPlayerAlive(UserListType type=UserListType.Game){lock(_gate){foreach(var u in Users(type).Values)if(u.IsPlaying&&u.IsDie)return false;return true;}}
    public bool IsAllPlayerDie(UserListType type=UserListType.Game){lock(_gate){foreach(var u in Users(type).Values)if(u.IsPlaying&&!u.IsDie)return false;return true;}}
    public bool IsAllPlayerHPReported(UserListType type=UserListType.Game){lock(_gate){foreach(var u in Users(type).Values)if(u.StateMachine.State==RoomUserState.Play&&u.HP<0f)return false;return true;}}
    public bool IsAllPlayerStageId(UserListType type=UserListType.Game){lock(_gate){foreach(var u in Users(type).Values)if(u.IsPlaying&&!u.IsDie&&u.StageId==-1)return false;return true;}}
    public bool IsAllPlayerSuccessResult(UserListType type=UserListType.Game){lock(_gate){foreach(var u in Users(type).Values){if(u.IsPvpNpc)continue;if(u.StateMachine.State!=RoomUserState.Result)continue;if(!u.IsSuccessResult)return false;}return true;}}
    public bool IsAnyTeamEliminated(){lock(_gate){var map=new Dictionary<int,bool>();foreach(var u in _gameUsers.Values){if(!map.TryGetValue(u.Team,out var allDie))map[u.Team]=u.IsDie;else if(!u.IsDie)map[u.Team]=false;}return map.Values.Any(x=>x);}}
    public bool IsOneTeamExist(UserListType type=UserListType.Game){lock(_gate){var users=Users(type);if(users.Count==0)return true;var first=users.Values.First();return users.Values.All(u=>u.Team==first.Team);}}
    public bool IsAnyoneReachObjectiveNumKill(int kill){lock(_gate)return _gameUsers.Values.Any(u=>u.IsPlaying&&u.NumKill>=kill);}
    public bool IsAnyTeamReachObjectiveNumKill(int kill){lock(_gate)return _teamNumKill.Values.Any(v=>v>=kill);}
    public int GetMaxKillUnit(){lock(_gate)return _gameUsers.Values.Where(u=>u.IsPlaying).Select(u=>u.NumKill).DefaultIfEmpty(0).Max();}
    public int GetMaxKillTeam(){lock(_gate)return _teamNumKill.Values.DefaultIfEmpty(0).Max();}
    public int GetTeamScore(int team){lock(_gate)return _teamNumKill.GetValueOrDefault(team);}
    public void AddTeamKill(int team,int amount=1){lock(_gate)_teamNumKill[team]=_teamNumKill.GetValueOrDefault(team)+amount;}
    public bool OpenSlot(int slotId,UserListType type=UserListType.Game){lock(_gate)return GetSlotUnsafe(slotId,type)?.Open()==true;}
    public bool CloseSlot(int slotId,UserListType type=UserListType.Game){lock(_gate)return GetSlotUnsafe(slotId,type)?.Close()==true;}
    public bool ToggleOpenClose(int slotId,UserListType type=UserListType.Game){lock(_gate)return GetSlotUnsafe(slotId,type)?.ToggleOpenClose()==true;}
    public bool OpenSlotTeam(int slotId,out int pairedSlotId,UserListType type=UserListType.Game)=>SetPairedSlot(slotId,true,out pairedSlotId,type);
    public bool CloseSlotTeam(int slotId,out int pairedSlotId,UserListType type=UserListType.Game)=>SetPairedSlot(slotId,false,out pairedSlotId,type);
    public IReadOnlyList<RoomSlotInfo> GetRoomSlotInfo(UserListType type=UserListType.Game){lock(_gate)return Slots(type).Select(x=>x.GetRoomSlotInfo()).ToArray();}
    public bool GetRoomUserGs(long unitUid,out long gsUid){lock(_gate){var u=GetUserUnsafe(unitUid,UserListType.Game);if(u is null){gsUid=0;return false;}gsUid=u.GSUid;return true;}}
    public Dictionary<long,HashSet<long>> GetUserList(int flag,UserListType type=UserListType.Game){lock(_gate){var result=new Dictionary<long,HashSet<long>>();foreach(var u in Users(type).Values){if(u.IsPvpNpc)continue;var include=flag switch{0=>true,1=>u.Team==0,2=>u.Team==1,3=>u.IsPlaying,4=>u.StateMachine.State==RoomUserState.Result,_=>false};if(!include)continue;if(!result.TryGetValue(u.GSUid,out var ids))result[u.GSUid]=ids=new();ids.Add(u.UnitUid);}return result;}}
    public void AssignTeam(int gameMode){lock(_gate){_gameMode=gameMode;AssignTeamInternal(_gameSlots);AssignTeamInternal(_observerSlots);}}
    public bool DecideWinTeam(byte gameType,out List<int> winTeams){lock(_gate){winTeams=new();if(gameType==1){var scores=new Dictionary<int,(int alive,float hp)>();foreach(var u in _gameUsers.Values){var s=scores.GetValueOrDefault(u.Team);scores[u.Team]=(s.alive+(u.IsDie?0:1),s.hp+(u.HP>0?u.HP:0));}var bestAlive=scores.Values.Select(x=>x.alive).DefaultIfEmpty(-1).Max();var bestHp=scores.Where(x=>x.Value.alive==bestAlive).Select(x=>x.Value.hp).DefaultIfEmpty(-1).Max();winTeams.AddRange(scores.Where(x=>x.Value.alive==bestAlive&&x.Value.hp==bestHp).Select(x=>x.Key));return true;}if(gameType==2){var scores=new Dictionary<int,int>();foreach(var u in _gameUsers.Values)if(u.IsPlaying)scores[u.Team]=scores.GetValueOrDefault(u.Team)+u.NumKill;var best=scores.Values.DefaultIfEmpty(-1).Max();winTeams.AddRange(scores.Where(x=>x.Value==best).Select(x=>x.Key));return true;}if(gameType==3){var best=_gameUsers.Values.Where(u=>u.StateMachine.State==RoomUserState.Result).Select(u=>u.NumKill).DefaultIfEmpty(-1).Max();winTeams.AddRange(_gameUsers.Values.Where(u=>u.StateMachine.State==RoomUserState.Result&&u.NumKill==best).Select(u=>u.Team).Distinct());return true;}return false;}}
    public void Reset(UserListType type=UserListType.Game){lock(_gate)ResetInternal(type);}
    private List<RoomSlot> Slots(UserListType type)=>type==UserListType.Game?_gameSlots:_observerSlots;
    private Dictionary<long,RoomUser> Users(UserListType type)=>type==UserListType.Game?_gameUsers:_observerUsers;
    private RoomUser? GetUserUnsafe(long id,UserListType type)=>Users(type).TryGetValue(id,out var u)?u:null;
    private RoomSlot? GetSlotUnsafe(int id,UserListType type){var s=Slots(type);return id>=0&&id<s.Count?s[id]:null;}
    private int GetNumOccupiedSlotUnsafe(UserListType type)=>Slots(type).Count(x=>x.IsOccupied);
    private bool AddUserUnsafe(RoomUser user,UserListType type){var users=Users(type);if(users.ContainsKey(user.UnitUid))return false;users[user.UnitUid]=user;return true;}
    private bool DeleteUserUnsafe(long unitUid,UserListType type){var users=Users(type);if(!users.TryGetValue(unitUid,out var user))return true;var slot=Slots(type).FirstOrDefault(x=>ReferenceEquals(x.User,user));if(slot is not null&&!slot.Leave())return false;return users.Remove(unitUid);}
    private void ResetInternal(UserListType type){foreach(var s in Slots(type))s.ResetSlot();Users(type).Clear();if(type==UserListType.Game)_teamNumKill.Clear();}
    private void AssignTeamInternal(IEnumerable<RoomSlot> slots){foreach(var s in slots)s.AssignTeam(_gameMode);}
    private static int ChooseTeamInternal(Dictionary<long,RoomUser> users){var red=users.Values.Count(x=>x.Team==0);var blue=users.Values.Count(x=>x.Team==1);return red<=blue?0:1;}
    private static RoomSlot? FindEmptyTeamSlotInternal(IEnumerable<RoomSlot> slots,int team)=>slots.FirstOrDefault(x=>x.IsOpened&&!x.IsOccupied&&x.Team==team);
    private bool SetPairedSlot(int slotId,bool open,out int pairedSlotId,UserListType type){lock(_gate){pairedSlotId=0;var slots=Slots(type);var first=GetSlotUnsafe(slotId,type);if(first is null)return false;if(open?(first.IsOpened||first.IsOccupied):(!first.IsOpened||first.IsOccupied))return false;var half=slots.Count/2;var start=slotId<half?half:0;var end=slotId<half?slots.Count:half;var second=Enumerable.Range(start,end-start).Select(i=>slots[i]).FirstOrDefault(s=>open?!s.IsOpened&&!s.IsOccupied:s.IsOpened&&!s.IsOccupied);if(second is null)return false;pairedSlotId=second.SlotId;return open?first.Open()&&second.Open():first.Close()&&second.Close();}}
    private bool AppointNewHostUnsafe(UserListType type,long oldHost){var users=Users(type);foreach(var u in users.Values)if(u.UnitUid!=oldHost){u.SetHost(true);return true;}return false;}
}