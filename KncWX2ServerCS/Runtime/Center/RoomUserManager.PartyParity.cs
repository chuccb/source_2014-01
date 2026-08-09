namespace KncWX2Server.Runtime.Center;

public static class RoomUserManagerPartyParity
{
    /// <summary>Native IsExistCharType: searches only game users for the requested unit type.</summary>
    public static bool IsExistCharType(this RoomUserManager manager, int unitType)
        => EnumerateGameUsers(manager).Any(user => user.UnitType == unitType);

    /// <summary>Native SetUnitLevelBeforGameStart stores the suitability result by CID.</summary>
    public static void SetUnitLevelBeforGameStart(this RoomUserManager manager, long unitUid, bool suitableLevel)
        => SuitableLevelState.Set(manager, unitUid, suitableLevel);

    public static bool TryGetUnitLevelBeforeGameStart(this RoomUserManager manager, long unitUid, out bool suitableLevel)
        => SuitableLevelState.TryGet(manager, unitUid, out suitableLevel);

    private static IEnumerable<RoomUser> EnumerateGameUsers(RoomUserManager manager)
    {
        foreach (var unitUid in manager.GetUserList(0, RoomUserManager.UserListType.Game).Values)
        {
            var user = manager.GetUser(unitUid);
            if (user is not null) yield return user;
        }
    }

    private static class SuitableLevelState
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RoomUserManager, System.Collections.Concurrent.ConcurrentDictionary<long, bool>> Values = new();

        public static void Set(RoomUserManager manager, long unitUid, bool value)
            => Values.GetOrCreateValue(manager)[unitUid] = value;

        public static bool TryGet(RoomUserManager manager, long unitUid, out bool value)
            => Values.GetOrCreateValue(manager).TryGetValue(unitUid, out value);
    }
}