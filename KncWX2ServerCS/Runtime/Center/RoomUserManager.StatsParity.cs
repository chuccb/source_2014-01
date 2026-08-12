namespace KncWX2Server.Runtime.Center;

public static class RoomUserManagerStatsParity
{
    /// <summary>Native GetUsedRessurectionStoneCount: missing users return false/zero in native's int return path.</summary>
    public static int GetUsedRessurectionStoneCount(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.UsedResurrectionStoneCount ?? 0;

    /// <summary>Native GetUsedRessurectionStonePlayerCount: count game users with at least one resurrection stone use.</summary>
    public static int GetUsedRessurectionStonePlayerCount(this RoomUserManager manager)
        => EnumerateGameUsers(manager).Count(user => user.UsedResurrectionStoneCount > 0);

    public static void SetUsedRessurectionStoneCount(this RoomUserManager manager, long unitUid, int count)
    {
        var user = manager.GetUser(unitUid);
        user?.SetUsedResurrectionStoneCount(count);
    }

    public static bool IsGameBang(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.IsGameBang == true;

    public static int GetPcBangType(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.PcBangType ?? -1;

    public static bool SetBattleFieldNpcLoad(this RoomUserManager manager, long unitUid, bool value)
        => manager.GetUser(unitUid)?.SetBattleFieldNpcLoad(value) == true;

    public static bool GetBattleFieldNpcLoad(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.BattleFieldNpcLoad == true;

    public static bool SetBattleFieldNpcSyncSubjects(this RoomUserManager manager, long unitUid, bool value)
        => manager.GetUser(unitUid)?.SetBattleFieldNpcSyncSubjects(value) == true;

    public static bool GetBattleFieldNpcSyncSubjects(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.BattleFieldNpcSyncSubjects == true;

    public static bool SetHenirReward(this RoomUserManager manager, long unitUid, bool value)
        => manager.GetUser(unitUid)?.SetHenirReward(value) == true;

    public static bool IsHenirReward(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.IsHenirReward == true;

    public static void ClearPingScore(this RoomUserManager manager)
    {
        foreach (var user in EnumerateGameUsers(manager))
            user.SetReceivedPingScore(false);
    }

    public static bool SetReceivedPingScore(this RoomUserManager manager, long unitUid, bool value)
        => manager.GetUser(unitUid)?.SetReceivedPingScore(value) == true;

    public static bool GetReceivedPingScore(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.ReceivedPingScore == true;

    public static bool SetZombieAlert(this RoomUserManager manager, long unitUid, bool value)
        => manager.GetUser(unitUid)?.SetZombieAlert(value) == true;

    public static bool GetZombieAlert(this RoomUserManager manager, long unitUid)
        => manager.GetUser(unitUid)?.ZombieAlert == true;

    public static bool SetEndPlay(this RoomUserManager manager, long unitUid, bool value)
        => manager.GetUser(unitUid)?.SetEndPlay(value) == true;

    public static bool SetCashContinueReady(this RoomUserManager manager, long unitUid, bool value)
        => manager.GetUser(unitUid)?.SetCashContinueReady(value) == true;

    private static IEnumerable<RoomUser> EnumerateGameUsers(RoomUserManager manager)
    {
        foreach (var unitUid in EnumerateGameUnitUids(manager))
        {
            var user = manager.GetUser(unitUid);
            if (user is not null) yield return user;
        }
    }
}
