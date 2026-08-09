namespace KncWX2Server.Runtime.Center;

public static class RoomUserStatsParityExtensions
{
    public static bool IsLoadingTimeOut(this RoomUser user, double elapsedSeconds, int authLevel=0)
    {
        if(authLevel>=1) return false;
        if(user.LoadingProgress==100) return false;
        return user.StateMachine.State==RoomUserState.Load && elapsedSeconds>=RoomUser.LoadingTimeoutSeconds;
    }

    public static int GetPercentHP(this RoomUser user, int baseHp)
        => user.GetPercentHP(baseHp);

    public static void IncreaseUsedResurrectionStoneCount(this RoomUser user)
        => user.SetUsedResurrectionStoneCount(user.UsedResurrectionStoneCount+1);

    public static void SetDungeonUnitInfo(this RoomUser user)
        => user.SetDungeonUnitInfoReceived(true);

    public static bool IsOnlyPlaying(this RoomUser user)
        => user.StateMachine.State==RoomUserState.Play;
}