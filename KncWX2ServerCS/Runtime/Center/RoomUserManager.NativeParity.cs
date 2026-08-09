namespace KncWX2Server.Runtime.Center;

/// <summary>
/// Small parity helpers whose native implementations are direct delegation/wrappers.
/// Complex reward/drop/zombie systems stay out until their dependent models are ported.
/// </summary>
public static class RoomUserManagerNativeParityExtensions
{
    public static int GetKillNumber(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.NumKill ?? 0;

    public static float CheckDungeonBalRate(this RoomUserManager manager, int unitLevel, int dungeonMinLevel)
    {
        var difference = dungeonMinLevel - unitLevel;
        return difference switch
        {
            <= 3 => 1.0f,
            4 => 0.8f,
            >= 5 and <= 6 => 0.6f,
            >= 7 and <= 9 => 0.5f,
            >= 10 and <= 12 => 0.4f,
            >= 13 and <= 15 => 0.2f,
            >= 16 and <= 19 => 0.1f,
            _ => 0.0f
        };
    }

    public static bool TryGetRewardEXP(this RoomUserManager manager, long unitUid, out int exp)
    {
        exp = 0;
        var user = manager.GetUser(unitUid);
        if (user is null) return false;
        exp = user.RewardEXP;
        return true;
    }

    public static bool TryGetRewardPartyEXP(this RoomUserManager manager, long unitUid, out int exp)
    {
        exp = 0;
        var user = manager.GetUser(unitUid);
        if (user is null) return false;
        exp = user.RewardPartyEXP;
        return true;
    }

    public static bool SetIsIntrude(this RoomUserManager manager, long unitUid, bool value, bool observer = false)
        => manager.GetUser(unitUid, observer ? RoomUserManager.UserListType.Observer : RoomUserManager.UserListType.Game)?.SetIsIntrude(value) == true;

    public static bool GetIsIntrude(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.IsIntrude == true;

    public static bool IsRingOfPvpRebirth(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.IsRingOfPvpRebirth == true;

    public static void ResetAgreeEnterSecretStage(this RoomUserManager manager)
    {
        foreach (var unitUid in EnumerateGameUnitUids(manager))
            manager.GetUser(unitUid)?.SetAgreeEnterSecretStage(RoomUser.SecretStageNone);
    }

    public static bool AgreeEnterSecretStage(this RoomUserManager manager, long unitUid, int agreeValue)
    {
        var user = manager.GetUser(unitUid);
        if (user is null) return false;
        user.SetAgreeEnterSecretStage(agreeValue);
        return true;
    }

    public static bool IsAllPlayerAgreed(this RoomUserManager manager)
    {
        var unitUids = EnumerateGameUnitUids(manager).ToArray();
        if (unitUids.Length == 0) return false;
        return unitUids.All(id => manager.GetUser(id)?.AgreeEnterSecretStage != RoomUser.SecretStageNone);
    }

    public static bool IsEnterSecretStage(this RoomUserManager manager)
    {
        var unitUids = EnumerateGameUnitUids(manager).ToArray();
        var agreeCount = unitUids.Count(id => manager.GetUser(id)?.AgreeEnterSecretStage == RoomUser.SecretStageAgree);
        return unitUids.Length <= agreeCount * 2;
    }

    public static bool IsExistPcBangPlayer(this RoomUserManager manager)
        => EnumerateGameUnitUids(manager).Any(id => manager.GetUser(id)?.IsGameBang == true);

    public static bool HavePet(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.HavePet == true;

    public static IReadOnlyList<long> GetTeamMemberList(this RoomUserManager manager, int team, bool playingOnly = false)
    {
        var result = new List<long>();
        foreach (var unitUid in EnumerateGameUnitUids(manager))
        {
            var user = manager.GetUser(unitUid);
            if (user is null || user.Team != team) continue;
            if (playingOnly && !user.IsPlaying) continue;
            result.Add(unitUid);
        }
        return result;
    }

    public static IReadOnlyList<long> GetUnitUIDList(this RoomUserManager manager, long excludedUnitUid = 0)
        => EnumerateGameUnitUids(manager)
            .Where(id => id != excludedUnitUid)
            .Distinct()
            .ToArray();

    public static int GetUserLevel(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.Level ?? 1;

    public static float GetPartyBonusRate(this RoomUserManager manager, int partyMemberCount)
        => partyMemberCount switch
        {
            1 => 0.0f,
            2 => 0.5f,
            3 => 1.0f,
            4 => 1.5f,
            _ => 0.0f
        };

    public static IReadOnlyList<long> GetLiveMemberList(this RoomUserManager manager)
        => EnumerateGameUnitUids(manager).Distinct().ToArray();

    public static bool SetRematch(this RoomUserManager manager, long unitUid, bool accept)
    {
        var user = manager.GetUser(unitUid);
        if (user is null) return false;
        user.SetRematch(accept);
        return true;
    }

    public static bool SetAllRematch(this RoomUserManager manager, bool accept)
    {
        foreach (var unitUid in EnumerateGameUnitUids(manager))
            manager.GetUser(unitUid)?.SetRematch(accept);
        return true;
    }

    public static bool IsAllPlayerWantRematch(this RoomUserManager manager)
    {
        var users = EnumerateGameUnitUids(manager).Select(manager.GetUser).Where(u => u is not null).Cast<RoomUser>().ToArray();
        if (users.Any(u => u.IsPvpNpc || !u.IsAcceptRematch)) return false;
        var red = users.Count(u => u.Team == 0);
        var blue = users.Count(u => u.Team == 1);
        return red == blue && red > 0;
    }

    public static void SetAllPrepareForDefenceDungeon(this RoomUserManager manager, bool value)
    {
        foreach (var unitUid in EnumerateGameUnitUids(manager))
            manager.GetUser(unitUid)?.SetPrepareForDefence(value);
    }

    public static bool SetPrepareForDefenceDungeon(this RoomUserManager manager, long unitUid, bool value)
    {
        var user = manager.GetUser(unitUid);
        if (user is null) return false;
        user.SetPrepareForDefence(value);
        return true;
    }

    public static bool IsAllPlayerPrepareForDefenceDungeon(this RoomUserManager manager)
    {
        var users = EnumerateGameUnitUids(manager).Select(manager.GetUser).Where(u => u is not null).Cast<RoomUser>().ToArray();
        return users.Length > 0 && users.All(u => u.IsPrepareForDefence);
    }

    public static bool SetEnterDefenceDungeon(this RoomUserManager manager, long unitUid, bool agree)
    {
        var user = manager.GetUser(unitUid);
        if (user is null) return false;
        user.SetRecvEnterPopupReply(true);
        user.SetEnterDefenceDungeon(agree);
        return true;
    }

    public static void SetAllEnterDefenceDungeon(this RoomUserManager manager)
    {
        foreach (var unitUid in EnumerateGameUnitUids(manager))
        {
            var user = manager.GetUser(unitUid);
            if (user is null || user.IsRecvEnterPopupReply) continue;
            user.SetRecvEnterPopupReply(true);
            user.SetEnterDefenceDungeon(true);
        }
    }

    public static bool SetMatchWaitTime(this RoomUserManager manager, long unitUid, int value)
    {
        var user = manager.GetUser(unitUid);
        if (user is null) return false;
        user.SetMatchWaitTime(value);
        return true;
    }

    public static bool SetAutoPartyWaitTime(this RoomUserManager manager, long unitUid, int value)
    {
        var user = manager.GetUser(unitUid);
        if (user is null) return false;
        user.SetAutoPartyWaitTime(value);
        return true;
    }

    public static bool IsPvpNpc(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.IsPvpNpc == true;

    public static bool IsOnlyPvpNpcInRoom(this RoomUserManager manager, out IReadOnlyList<long> npcUnitUids)
    {
        var users = EnumerateGameUnitUids(manager).Select(manager.GetUser).Where(u => u is not null).Cast<RoomUser>().ToArray();
        var ids = users.Where(u => u.IsPvpNpc).Select(u => u.Cid).ToArray();
        npcUnitUids = ids;
        return users.All(u => u.IsPvpNpc);
    }

    private static IEnumerable<long> EnumerateGameUnitUids(RoomUserManager manager)
    {
        foreach (var group in manager.GetUserList(0, RoomUserManager.UserListType.Game).Values)
            foreach (var unitUid in group)
                yield return unitUid;
    }
}