namespace KncWX2Server.Runtime.Center;

/// <summary>
/// Direct state-only counterparts for the public KRoomUserManager APIs that do not
/// require database, Lua, matchmaking, or dungeon-table dependencies.
/// </summary>
public static class RoomUserManagerStateParityExtensions
{
    public static RoomUser? GetUserByIndex(
        this RoomUserManager manager,
        int index,
        RoomUserManager.UserListType type = RoomUserManager.UserListType.Game)
    {
        ArgumentNullException.ThrowIfNull(manager);
        if (index < 0)
        {
            return null;
        }

        var users = GetUsers(manager, type)
            .OrderBy(static user => user.Cid)
            .ToArray();

        return index < users.Length ? users[index] : null;
    }

    public static int DeleteUserByGsUid(
        this RoomUserManager manager,
        long gsUid,
        ICollection<(long UnitUid, long PartyUid)> removed,
        RoomUserManager.UserListType type = RoomUserManager.UserListType.Game)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(removed);

        var matches = GetUsers(manager, type)
            .Where(user => user.GSUid == gsUid)
            .OrderBy(static user => user.Cid)
            .ToArray();

        foreach (var user in matches)
        {
            // Native KRoomUserManager::DeleteUserByGsUID only gathers the leave list;
            // actual deletion is performed by the caller after this method returns.
            removed.Add((user.Cid, user.PartyUid));
        }

        return matches.Length;
    }

    public static bool IsHost(this RoomUserManager manager, long cid)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return manager.GetUser(cid)?.IsHost == true;
    }

    public static bool IsPlaying(this RoomUserManager manager, long cid)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return manager.GetUser(cid)?.IsPlaying == true;
    }

    public static bool IsObserver(this RoomUserManager manager, long cid)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return manager.ObserverSlots.Any(slot => slot.User?.Cid == cid);
    }

    public static bool GetGsUid(this RoomUserManager manager, long cid, out long gsUid)
    {
        ArgumentNullException.ThrowIfNull(manager);
        var user = manager.GetUser(cid);
        gsUid = user?.GSUid ?? 0;
        return user is not null;
    }

    public static int GetTeamReadyNum(this RoomUserManager manager, int team)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return manager.GameSlots.Count(slot => slot.User is { Team: var userTeam, IsReady: true } && userTeam == team);
    }

    public static bool GetTeamNum(
        this RoomUserManager manager,
        out int redCount,
        out int blueCount)
    {
        ArgumentNullException.ThrowIfNull(manager);
        redCount = manager.GameSlots.Count(slot => slot.User?.Team == 0);
        blueCount = manager.GameSlots.Count(slot => slot.User?.Team == 1);
        return true;
    }

    public static void SetAllReady(this RoomUserManager manager, bool ready)
    {
        ArgumentNullException.ThrowIfNull(manager);
        foreach (var user in GetUsers(manager, RoomUserManager.UserListType.Game))
        {
            user.SetReady(ready);
        }
    }

    public static void ResetStageLoaded(this RoomUserManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        foreach (var user in GetAllUsers(manager))
        {
            user.SetStageLoaded(false);
        }
    }

    public static void ResetStageId(this RoomUserManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        foreach (var user in GetAllUsers(manager))
        {
            user.SetStage(-1);
            user.SetSubStage(-1);
        }
    }

    public static void ResetRebirthPos(this RoomUserManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        foreach (var user in GetAllUsers(manager))
        {
            user.SetRebirthPos(0);
        }
    }

    public static bool IsAllPlayerHpReported(this RoomUserManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        var users = GetUsers(manager, RoomUserManager.UserListType.Game).ToArray();
        return users.Length == 0 || users.All(static user =>
            user.StateMachine.State is not RoomUserState.Play || user.HP >= 0f);
    }

    public static bool IsAllPlayerStageId(this RoomUserManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        var users = GetUsers(manager, RoomUserManager.UserListType.Game).ToArray();
        return users.Length == 0 || users.All(static user =>
            !user.IsPlaying || user.IsDie || user.StageId != -1);
    }

    public static bool IsAllPlayerDungeonUnitInfoSet(this RoomUserManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        var users = GetUsers(manager, RoomUserManager.UserListType.Game).ToArray();
        return users.Length == 0 || users.All(static user =>
            user.StateMachine.State is not RoomUserState.Play || user.DungeonUnitInfoReceived);
    }

    public static bool GetRoomUserGs(
        this RoomUserManager manager,
        long cid,
        out long gsUid)
        => manager.GetGsUid(cid, out gsUid);

    public static IReadOnlyList<(long Cid, int Kill, int Die, int MdKill)> GetCurrentKillScore(
        this RoomUserManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return GetUsers(manager, RoomUserManager.UserListType.Game)
            .Select(static user => (user.Cid, user.NumKill, user.NumDie, user.NumMDKill))
            .OrderBy(static value => value.Cid)
            .ToArray();
    }

    public static IReadOnlyList<(long Cid, int Team, bool Ready)> GetTeamStateSnapshot(
        this RoomUserManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return GetUsers(manager, RoomUserManager.UserListType.Game)
            .Select(static user => (user.Cid, user.Team, user.IsReady))
            .OrderBy(static value => value.Cid)
            .ToArray();
    }

    public static RoomSlotInfo? GetRoomSlotInfo(
        this RoomUserManager manager,
        long cid,
        RoomUserManager.UserListType type = RoomUserManager.UserListType.Game)
    {
        ArgumentNullException.ThrowIfNull(manager);
        var user = manager.GetUser(cid, type);
        return user is null
            ? null
            : manager.GetRoomSlotInfo(type).FirstOrDefault(info => info.UnitUid == user.UnitUid);
    }

    private static IEnumerable<RoomUser> GetUsers(
        RoomUserManager manager,
        RoomUserManager.UserListType type)
    {
        var slots = type == RoomUserManager.UserListType.Observer
            ? manager.ObserverSlots
            : manager.GameSlots;

        return slots
            .Where(static slot => slot.User is not null)
            .Select(static slot => slot.User!);
    }

    private static IEnumerable<RoomUser> GetAllUsers(RoomUserManager manager) =>
        GetUsers(manager, RoomUserManager.UserListType.Game)
            .Concat(GetUsers(manager, RoomUserManager.UserListType.Observer));
}
