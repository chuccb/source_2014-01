namespace KncWX2Server.Runtime.Center;

public sealed class RoomUserManager
{
    public enum UserListType
    {
        Game = 0,
        Observer = 1,
    }

    private readonly List<RoomSlot> _gameSlots = [];
    private readonly List<RoomSlot> _observerSlots = [];
    private readonly Dictionary<long, RoomUser> _gameUsers = [];
    private readonly Dictionary<long, RoomUser> _observerUsers = [];
    private readonly object _gate = new();
    private readonly Dictionary<int, int> _teamNumKill = [];
    private int _gameMode;

    public IReadOnlyList<RoomSlot> GameSlots => _gameSlots;
    public IReadOnlyList<RoomSlot> ObserverSlots => _observerSlots;

    public void Init(int gameSlotCount, int observerSlotCount = 3, int gameMode = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gameSlotCount);
        ArgumentOutOfRangeException.ThrowIfNegative(observerSlotCount);

        lock (_gate)
        {
            ResetInternal(UserListType.Game);
            ResetInternal(UserListType.Observer);
            _gameMode = gameMode;

            for (var i = 0; i < gameSlotCount; i++)
            {
                _gameSlots.Add(new RoomSlot(i));
            }

            for (var i = 0; i < observerSlotCount; i++)
            {
                _observerSlots.Add(new RoomSlot(i));
            }

            AssignTeamInternal(_gameSlots);
            AssignTeamInternal(_observerSlots);
        }
    }

    public RoomSlot? GetSlot(int slotId, UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return GetSlotUnsafe(slotId, type);
        }
    }

    public RoomUser? GetUser(long unitUid, UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return GetUserUnsafe(unitUid, type);
        }
    }

    public int GetNumTotalSlot(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Slots(type).Count;
        }
    }

    public int GetNumOpenedSlot(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Slots(type).Count(slot => slot.IsOpened);
        }
    }

    public int GetNumOccupiedSlot(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Slots(type).Count(slot => slot.IsOccupied);
        }
    }

    public int GetNumMember(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Count;
        }
    }

    public int GetNumPlaying(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.Count(user => user.IsPlaying);
        }
    }

    public int GetNumResultPlayer(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.Count(user => user.StateMachine.State is RoomUserState.Result);
        }
    }

    public int GetNumReadyPlayer(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.Count(user => user.IsReady);
        }
    }

    public int GetLiveMember(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            var count = Users(type).Values.Count(user => !user.IsDie);
            return count > 0 ? count : 1;
        }
    }

    public int GetTeamNumPlaying(int team, UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.Count(user => user.Team == team && user.IsPlaying);
        }
    }

    public bool AddUser(RoomUser user, UserListType type = UserListType.Game)
    {
        ArgumentNullException.ThrowIfNull(user);

        lock (_gate)
        {
            return AddUserUnsafe(user, type);
        }
    }

    public bool DeleteUser(long unitUid, UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return DeleteUserUnsafe(unitUid, type);
        }
    }

    public bool EnterRoom(RoomUser user, bool considerTeam = true, UserListType type = UserListType.Game)
    {
        ArgumentNullException.ThrowIfNull(user);

        lock (_gate)
        {
            var users = Users(type);
            if (users.ContainsKey(user.UnitUid))
            {
                return false;
            }

            var slots = Slots(type);
            var slot = considerTeam
                ? FindEmptyTeamSlotInternal(slots, ChooseTeamInternal(users))
                : null;

            slot ??= slots.FirstOrDefault(candidate => candidate.IsOpened && !candidate.IsOccupied);
            if (slot is null || !AddUserUnsafe(user, type))
            {
                return false;
            }

            if (!slot.Enter(user))
            {
                users.Remove(user.UnitUid);
                return false;
            }

            if (type is UserListType.Game && GetNumOccupiedSlotUnsafe(type) == 1)
            {
                user.SetHost(true);
            }

            return true;
        }
    }

    public bool LeaveRoom(long unitUid, UserListType type = UserListType.Game) =>
        DeleteUser(unitUid, type);

    public void LeaveAllUnit()
    {
        lock (_gate)
        {
            var gameIds = _gameUsers.Keys.ToArray();
            var observerIds = _observerUsers.Keys.ToArray();

            foreach (var id in gameIds)
            {
                DeleteUserUnsafe(id, UserListType.Game);
            }

            foreach (var id in observerIds)
            {
                DeleteUserUnsafe(id, UserListType.Observer);
            }
        }
    }

    public bool LeaveGame(long unitUid)
    {
        lock (_gate)
        {
            var type = UserListType.Game;
            var user = GetUserUnsafe(unitUid, type);

            if (user is null)
            {
                type = UserListType.Observer;
                user = GetUserUnsafe(unitUid, type);
            }

            if (user is null)
            {
                return false;
            }

            var wasHost = user.IsHost;
            user.EndGame();

            if (type is UserListType.Game && wasHost && Users(type).Count > 1)
            {
                if (!AppointNewHostUnsafe(type, user.UnitUid))
                {
                    return false;
                }

                user.SetHost(false);
            }

            return true;
        }
    }

    public bool ChangeTeam(long unitUid, int destinationTeam, UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            var user = GetUserUnsafe(unitUid, type);
            if (user is null)
            {
                return false;
            }

            var source = Slots(type).FirstOrDefault(slot => ReferenceEquals(slot.User, user));
            if (source is null)
            {
                return false;
            }

            if (source.Team == destinationTeam)
            {
                return true;
            }

            var destination = Slots(type).FirstOrDefault(slot =>
                slot.IsOpened && !slot.IsOccupied && slot.Team == destinationTeam);

            if (destination is null || !source.Leave())
            {
                return false;
            }

            return destination.Enter(user);
        }
    }

    public bool SetReady(long unitUid, bool ready, UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return GetUserUnsafe(unitUid, type)?.SetReady(ready) == true;
        }
    }

    public bool SetAllReady(bool ready)
    {
        lock (_gate)
        {
            foreach (var user in _gameUsers.Values)
            {
                user.SetReady(ready);
            }

            return true;
        }
    }

    public bool SetPitIn(long unitUid, bool value, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, user => user.SetPitIn(value));

    public bool SetTrade(long unitUid, bool value, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, user => user.SetTrade(value));

    public bool SetLoadingProgress(long unitUid, int value, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, user => user.SetLoadingProgress(value));

    public bool SetStageLoaded(long unitUid, bool value, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, user => user.SetStageLoaded(value));

    public void ResetStageLoaded()
    {
        lock (_gate)
        {
            foreach (var user in _gameUsers.Values)
            {
                user.SetStageLoaded(false);
            }
        }
    }

    public bool SetDie(long unitUid, bool value, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, user => user.SetDie(value));

    public bool SetHP(long unitUid, float value, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, user => user.SetHP(value));

    public bool IncreaseNumKill(long unitUid, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, static user => user.IncreaseKill());

    public bool IncreaseNumMDKill(long unitUid, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, static user => user.IncreaseMDKill());

    public bool IncreaseNumDie(long unitUid, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, static user => user.IncreaseDie());

    public bool IncreaseTeamNumKill(long unitUid)
    {
        lock (_gate)
        {
            var user = GetUserUnsafe(unitUid, UserListType.Game);
            if (user is null)
            {
                return false;
            }

            _teamNumKill[user.Team] = _teamNumKill.GetValueOrDefault(user.Team) + 1;
            return true;
        }
    }

    public bool SetStageId(long unitUid, int value, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, user => user.SetStage(value));

    public bool SetSubStageId(long unitUid, int value, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, user => user.SetSubStage(value));

    public bool SetRebirthPos(long unitUid, int value, UserListType type = UserListType.Game) =>
        SetUserValue(unitUid, type, user => user.SetRebirthPos(value));

    public void StartGame(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            foreach (var user in Users(type).Values)
            {
                if (user.IsReady)
                {
                    user.StartGame();
                }
            }
        }
    }

    public void StartPlay(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            foreach (var user in Users(type).Values)
            {
                if (user.StateMachine.State is RoomUserState.Load)
                {
                    user.StartPlay();
                }
            }

            _teamNumKill.Clear();
        }
    }

    public void StartResult(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            foreach (var user in Users(type).Values)
            {
                if (user.StateMachine.State is RoomUserState.Play)
                {
                    user.EndPlay();
                }
            }
        }
    }

    public void EndPlay(UserListType type = UserListType.Game) => StartResult(type);

    public void EndGame(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            foreach (var user in Users(type).Values)
            {
                if (user.StateMachine.State is RoomUserState.Result)
                {
                    user.EndGame();
                }
            }
        }
    }

    public bool IsAllPlayerReady(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.All(user => user.IsReady);
        }
    }

    public bool IsAllPlayerFinishLoading(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.All(user =>
                !user.IsPlaying || user.LoadingProgress < 0 || user.LoadingProgress >= 100);
        }
    }

    public bool IsAllPlayerStageLoaded(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.All(user => !user.IsPlaying || user.IsStageLoaded);
        }
    }

    public bool IsAllPlayerAlive(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.All(user => !user.IsPlaying || !user.IsDie);
        }
    }

    public bool IsAllPlayerDie(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.All(user => !user.IsPlaying || user.IsDie);
        }
    }

    public bool IsAllPlayerHPReported(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.All(user =>
                user.StateMachine.State is not RoomUserState.Play || user.HP >= 0f);
        }
    }

    public bool IsAllPlayerStageId(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.All(user => !user.IsPlaying || user.IsDie || user.StageId != -1);
        }
    }

    public bool IsAllPlayerSuccessResult(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Users(type).Values.All(user =>
                user.IsPvpNpc || user.StateMachine.State is not RoomUserState.Result || user.IsSuccessResult);
        }
    }

    public bool IsAnyTeamEliminated()
    {
        lock (_gate)
        {
            var teamAlive = new Dictionary<int, bool>();

            foreach (var user in _gameUsers.Values)
            {
                if (!teamAlive.TryGetValue(user.Team, out var allDie))
                {
                    teamAlive[user.Team] = user.IsDie;
                    continue;
                }

                if (!user.IsDie)
                {
                    teamAlive[user.Team] = false;
                }
            }

            return teamAlive.Values.Any(allDie => allDie);
        }
    }

    public bool IsOneTeamExist(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            var users = Users(type);
            return users.Count == 0 || users.Values.All(user => user.Team == users.Values.First().Team);
        }
    }

    public bool IsAnyoneReachObjectiveNumKill(int kill)
    {
        lock (_gate)
        {
            return _gameUsers.Values.Any(user => user.IsPlaying && user.NumKill >= kill);
        }
    }

    public bool IsAnyTeamReachObjectiveNumKill(int kill)
    {
        lock (_gate)
        {
            return _teamNumKill.Values.Any(score => score >= kill);
        }
    }

    public int GetMaxKillUnit()
    {
        lock (_gate)
        {
            return _gameUsers.Values
                .Where(user => user.IsPlaying)
                .Select(user => user.NumKill)
                .DefaultIfEmpty(0)
                .Max();
        }
    }

    public int GetMaxKillTeam()
    {
        lock (_gate)
        {
            return _teamNumKill.Values.DefaultIfEmpty(0).Max();
        }
    }

    public int GetTeamScore(int team)
    {
        lock (_gate)
        {
            return _teamNumKill.GetValueOrDefault(team);
        }
    }

    public void AddTeamKill(int team, int amount = 1)
    {
        lock (_gate)
        {
            _teamNumKill[team] = _teamNumKill.GetValueOrDefault(team) + amount;
        }
    }

    public bool OpenSlot(int slotId, UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return GetSlotUnsafe(slotId, type)?.Open() == true;
        }
    }

    public bool CloseSlot(int slotId, UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return GetSlotUnsafe(slotId, type)?.Close() == true;
        }
    }

    public bool ToggleOpenClose(int slotId, UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return GetSlotUnsafe(slotId, type)?.ToggleOpenClose() == true;
        }
    }

    public bool OpenSlotTeam(int slotId, out int pairedSlotId, UserListType type = UserListType.Game) =>
        SetPairedSlot(slotId, true, out pairedSlotId, type);

    public bool CloseSlotTeam(int slotId, out int pairedSlotId, UserListType type = UserListType.Game) =>
        SetPairedSlot(slotId, false, out pairedSlotId, type);

    public IReadOnlyList<RoomSlotInfo> GetRoomSlotInfo(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            return Slots(type).Select(slot => slot.GetRoomSlotInfo()).ToArray();
        }
    }

    public bool GetRoomUserGs(long unitUid, out long gsUid)
    {
        lock (_gate)
        {
            var user = GetUserUnsafe(unitUid, UserListType.Game);
            if (user is null)
            {
                gsUid = 0;
                return false;
            }

            gsUid = user.GSUid;
            return true;
        }
    }

    public Dictionary<long, HashSet<long>> GetUserList(int flag, UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            var result = new Dictionary<long, HashSet<long>>();

            foreach (var user in Users(type).Values)
            {
                if (user.IsPvpNpc)
                {
                    continue;
                }

                var include = flag switch
                {
                    0 => true,
                    1 => user.Team == 0,
                    2 => user.Team == 1,
                    3 => user.IsPlaying,
                    4 => user.StateMachine.State is RoomUserState.Result,
                    _ => false,
                };

                if (!include)
                {
                    continue;
                }

                if (!result.TryGetValue(user.GSUid, out var ids))
                {
                    ids = [];
                    result[user.GSUid] = ids;
                }

                ids.Add(user.UnitUid);
            }

            return result;
        }
    }

    public void AssignTeam(int gameMode)
    {
        lock (_gate)
        {
            _gameMode = gameMode;
            AssignTeamInternal(_gameSlots);
            AssignTeamInternal(_observerSlots);
        }
    }

    public bool DecideWinTeam(byte gameType, out List<int> winTeams)
    {
        lock (_gate)
        {
            winTeams = [];

            switch (gameType)
            {
                case 1:
                    var scores = new Dictionary<int, (int alive, float hp)>();
                    foreach (var user in _gameUsers.Values)
                    {
                        var score = scores.GetValueOrDefault(user.Team);
                        scores[user.Team] = (
                            score.alive + (user.IsDie ? 0 : 1),
                            score.hp + (user.HP > 0 ? user.HP : 0));
                    }

                    var bestAlive = scores.Values.Select(score => score.alive).DefaultIfEmpty(-1).Max();
                    var bestHp = scores
                        .Where(pair => pair.Value.alive == bestAlive)
                        .Select(pair => pair.Value.hp)
                        .DefaultIfEmpty(-1)
                        .Max();

                    winTeams.AddRange(
                        scores
                            .Where(pair => pair.Value.alive == bestAlive && pair.Value.hp == bestHp)
                            .Select(pair => pair.Key));
                    return true;

                case 2:
                    var teamScores = new Dictionary<int, int>();
                    foreach (var user in _gameUsers.Values.Where(user => user.IsPlaying))
                    {
                        teamScores[user.Team] = teamScores.GetValueOrDefault(user.Team) + user.NumKill;
                    }

                    var bestTeamScore = teamScores.Values.DefaultIfEmpty(-1).Max();
                    winTeams.AddRange(
                        teamScores
                            .Where(pair => pair.Value == bestTeamScore)
                            .Select(pair => pair.Key));
                    return true;

                case 3:
                    var bestKill = _gameUsers.Values
                        .Where(user => user.StateMachine.State is RoomUserState.Result)
                        .Select(user => user.NumKill)
                        .DefaultIfEmpty(-1)
                        .Max();

                    winTeams.AddRange(
                        _gameUsers.Values
                            .Where(user => user.StateMachine.State is RoomUserState.Result && user.NumKill == bestKill)
                            .Select(user => user.Team)
                            .Distinct());
                    return true;

                default:
                    return false;
            }
        }
    }

    public void Reset(UserListType type = UserListType.Game)
    {
        lock (_gate)
        {
            ResetInternal(type);
        }
    }

    private List<RoomSlot> Slots(UserListType type) =>
        type is UserListType.Game ? _gameSlots : _observerSlots;

    private Dictionary<long, RoomUser> Users(UserListType type) =>
        type is UserListType.Game ? _gameUsers : _observerUsers;

    private RoomUser? GetUserUnsafe(long id, UserListType type) =>
        Users(type).TryGetValue(id, out var user) ? user : null;

    private RoomSlot? GetSlotUnsafe(int id, UserListType type)
    {
        var slots = Slots(type);
        return (uint)id < (uint)slots.Count ? slots[id] : null;
    }

    private int GetNumOccupiedSlotUnsafe(UserListType type) =>
        Slots(type).Count(slot => slot.IsOccupied);

    private bool AddUserUnsafe(RoomUser user, UserListType type)
    {
        var users = Users(type);
        if (!users.TryAdd(user.UnitUid, user))
        {
            return false;
        }

        return true;
    }

    private bool DeleteUserUnsafe(long unitUid, UserListType type)
    {
        var users = Users(type);
        if (!users.TryGetValue(unitUid, out var user))
        {
            return true;
        }

        var slot = Slots(type).FirstOrDefault(candidate => ReferenceEquals(candidate.User, user));
        if (slot is not null && !slot.Leave())
        {
            return false;
        }

        return users.Remove(unitUid);
    }

    private void ResetInternal(UserListType type)
    {
        foreach (var slot in Slots(type))
        {
            slot.ResetSlot();
        }

        Users(type).Clear();

        if (type is UserListType.Game)
        {
            _teamNumKill.Clear();
        }
    }

    private void AssignTeamInternal(IEnumerable<RoomSlot> slots)
    {
        foreach (var slot in slots)
        {
            slot.AssignTeam(_gameMode);
        }
    }

    private static int ChooseTeamInternal(Dictionary<long, RoomUser> users)
    {
        var red = users.Values.Count(user => user.Team == 0);
        var blue = users.Values.Count(user => user.Team == 1);
        return red <= blue ? 0 : 1;
    }

    private static RoomSlot? FindEmptyTeamSlotInternal(IEnumerable<RoomSlot> slots, int team) =>
        slots.FirstOrDefault(slot => slot.IsOpened && !slot.IsOccupied && slot.Team == team);

    private bool SetPairedSlot(int slotId, bool open, out int pairedSlotId, UserListType type)
    {
        lock (_gate)
        {
            pairedSlotId = 0;
            var slots = Slots(type);
            var first = GetSlotUnsafe(slotId, type);

            if (first is null)
            {
                return false;
            }

            var invalidState = open
                ? first.IsOpened || first.IsOccupied
                : !first.IsOpened || first.IsOccupied;

            if (invalidState)
            {
                return false;
            }

            var half = slots.Count / 2;
            var start = slotId < half ? half : 0;
            var end = slotId < half ? slots.Count : half;

            var second = Enumerable.Range(start, end - start)
                .Select(index => slots[index])
                .FirstOrDefault(slot =>
                    open ? !slot.IsOpened && !slot.IsOccupied : slot.IsOpened && !slot.IsOccupied);

            if (second is null)
            {
                return false;
            }

            pairedSlotId = second.SlotId;
            return open
                ? first.Open() && second.Open()
                : first.Close() && second.Close();
        }
    }

    private bool AppointNewHostUnsafe(UserListType type, long oldHost)
    {
        var users = Users(type);
        foreach (var user in users.Values)
        {
            if (user.UnitUid == oldHost)
            {
                continue;
            }

            user.SetHost(true);
            return true;
        }

        return false;
    }

    private bool SetUserValue(long unitUid, UserListType type, Action<RoomUser> update)
    {
        lock (_gate)
        {
            var user = GetUserUnsafe(unitUid, type);
            if (user is null)
            {
                return false;
            }

            update(user);
            return true;
        }
    }
}
