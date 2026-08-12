namespace KncWX2Server.Runtime.Center;

public static class RoomUserManagerParityHelpers
{
    public static IEnumerable<long> EnumerateGameUnitUids(RoomUserManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        return manager.GetUserList(0, RoomUserManager.UserListType.Game)
            .Values
            .SelectMany(static unitUids => unitUids)
            .Distinct()
            .Order();
    }
}
