namespace KncWX2Server.Runtime.Center;

/// <summary>
/// Small parity helpers whose native implementations are direct delegation/wrappers.
/// Complex reward/drop/zombie systems stay out until their dependent models are ported.
/// </summary>
public static class RoomUserManagerNativeParityExtensions
{
    /// <summary>Native GetKillNumber: missing user is reported as zero.</summary>
    public static int GetKillNumber(this RoomUserManager manager,long unitUid)
        => manager.GetUser(unitUid)?.NumKill??0;

    /// <summary>Native CheckDungeonBalRate, with the dungeon minimum level supplied by the dungeon service.</summary>
    public static float CheckDungeonBalRate(this RoomUserManager manager,int unitLevel,int dungeonMinLevel)
    {
        var difference=Math.Abs(dungeonMinLevel-unitLevel);
        return difference switch
        {
            <=3=>1.0f,
            4=>0.8f,
            >=5 and <=6=>0.6f,
            >=7 and <=9=>0.5f,
            >=10 and <=12=>0.4f,
            >=13 and <=15=>0.2f,
            >=16 and <=19=>0.1f,
            _=>0.0f
        };
    }

    /// <summary>Native GetRewardEXP wrapper; returns false when the room user does not exist.</summary>
    public static bool TryGetRewardEXP(this RoomUserManager manager,long unitUid,out int exp)
    {
        exp=0;
        var user=manager.GetUser(unitUid);
        if(user is null)return false;
        exp=user.RewardEXP;
        return true;
    }

    /// <summary>Native GetRewardPartyEXP wrapper; returns false when the room user does not exist.</summary>
    public static bool TryGetRewardPartyEXP(this RoomUserManager manager,long unitUid,out int exp)
    {
        exp=0;
        var user=manager.GetUser(unitUid);
        if(user is null)return false;
        exp=user.RewardPartyEXP;
        return true;
    }

    /// <summary>Native SetIsIntrude/GetIsIntrude delegation.</summary>
    public static bool SetIsIntrude(this RoomUserManager manager,long unitUid,bool value,bool observer=false)
        => manager.GetUser(unitUid,observer?RoomUserManager.UserListType.Observer:RoomUserManager.UserListType.Game)?.SetIsIntrude(value)==true;

    public static bool GetIsIntrude(this RoomUserManager manager,long unitUid)
        => manager.GetUser(unitUid)?.IsIntrude==true;

    /// <summary>Native IsRingofpvprebirth delegation.</summary>
    public static bool IsRingOfPvpRebirth(this RoomUserManager manager,long unitUid)
        => manager.GetUser(unitUid)?.IsRingOfPvpRebirth==true;
}
