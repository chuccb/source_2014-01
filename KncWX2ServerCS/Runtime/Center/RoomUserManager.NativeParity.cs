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

    private static IEnumerable<long> EnumerateGameUnitUids(RoomUserManager manager)
    {
        foreach (var group in manager.GetUserList(0, RoomUserManager.UserListType.Game).Values)
            foreach (var unitUid in group)
                yield return unitUid;
    }
}