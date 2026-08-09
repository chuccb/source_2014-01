namespace KncWX2Server.Runtime.Center;

/// <summary>Parity helpers whose native implementations are direct, dependency-free calculations/delegations.</summary>
public static class RoomUserManagerNativeParityExtensions
{
    /// <summary>Native GetKillNumber: a missing room user yields zero.</summary>
    public static int GetKillNumber(this RoomUserManager manager,long unitUid)
        => manager.GetUser(unitUid)?.NumKill??0;

    /// <summary>Native CheckDungeonBalRate. dungeonMinLevel is supplied by the dungeon subsystem.</summary>
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
}
