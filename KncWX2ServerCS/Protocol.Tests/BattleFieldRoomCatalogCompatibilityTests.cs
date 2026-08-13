using KncWX2Server.Runtime.BattleField;

internal static class BattleFieldRoomCatalogCompatibilityTests
{
    public static void Run()
    {
        TestAddRemoveLookup();
        TestPartyRoomPriority();
        TestPartyTieKeepsNativeOrder();
        TestTargetRoomFallback();
        TestRandomFallback();
        TestReservedCapacity();
        TestNoCandidateIsNotAnError();
        TestCapacityAndAlreadyJoined();
    }

    private static void TestAddRemoveLookup()
    {
        var manager = new BattleFieldRoomManager();
        var room = manager.Create("A", 4, 10);
        var catalog = new BattleFieldRoomCatalog();

        Assert(catalog.AddRoom(room));
        Assert(!catalog.AddRoom(room));
        Assert(catalog.BattleFieldCount == 1 && catalog.RoomCount == 1);
        Assert(catalog.TryGetRoom(10, room.RoomUid, out var found) && found!.RoomUid == room.RoomUid);
        Assert(catalog.GetRooms(10).Count == 1);
        Assert(catalog.RemoveRoom(10, room.RoomUid));
        Assert(!catalog.TryGetRoom(10, room.RoomUid, out _));
        Assert(catalog.BattleFieldCount == 0 && catalog.RoomCount == 0);
    }

    private static void TestPartyRoomPriority()
    {
        var manager = new BattleFieldRoomManager();
        var roomA = manager.Create("A", 4, 10);
        var roomB = manager.Create("B", 4, 10);
        var catalog = new BattleFieldRoomCatalog();
        catalog.AddRoom(roomA);
        catalog.AddRoom(roomB);

        var request = new BattleFieldRoomJoinRequest(10, 100, 0, 900);
        Assert(catalog.TrySelectRoom(
            request,
            out var selected,
            partyMemberCount: room => room.RoomUid == roomB.RoomUid ? 2 : 1,
            randomSelector: static rooms => rooms[0]));

        Assert(selected!.RoomUid == roomB.RoomUid);
    }

    private static void TestPartyTieKeepsNativeOrder()
    {
        var manager = new BattleFieldRoomManager();
        var first = manager.Create("First", 4, 10);
        var second = manager.Create("Second", 4, 10);
        var catalog = new BattleFieldRoomCatalog();
        catalog.AddRoom(first);
        catalog.AddRoom(second);

        var request = new BattleFieldRoomJoinRequest(10, 100, 0, 900);
        Assert(catalog.TrySelectRoom(
            request,
            out var selected,
            partyMemberCount: static _ => 2,
            randomSelector: static rooms => rooms[^1]));

        Assert(selected!.RoomUid == first.RoomUid);
    }

    private static void TestTargetRoomFallback()
    {
        var manager = new BattleFieldRoomManager();
        var target = manager.Create("Target", 4, 10);
        var other = manager.Create("Other", 4, 10);
        var catalog = new BattleFieldRoomCatalog();
        catalog.AddRoom(target);
        catalog.AddRoom(other);

        var request = new BattleFieldRoomJoinRequest(10, 100, target.RoomUid, 0);
        Assert(catalog.TrySelectRoom(
            request,
            out var selected,
            randomSelector: static rooms => rooms[0]));
        Assert(selected!.RoomUid == target.RoomUid);
    }

    private static void TestRandomFallback()
    {
        var manager = new BattleFieldRoomManager();
        var first = manager.Create("A", 4, 10);
        var second = manager.Create("B", 4, 10);
        var catalog = new BattleFieldRoomCatalog();
        catalog.AddRoom(first);
        catalog.AddRoom(second);

        var request = new BattleFieldRoomJoinRequest(10, 100, 0, 0);
        Assert(catalog.TrySelectRoom(
            request,
            out var selected,
            randomSelector: static rooms => rooms[^1]));
        Assert(selected!.RoomUid == second.RoomUid);
    }

    private static void TestReservedCapacity()
    {
        var manager = new BattleFieldRoomManager();
        var reservedRoom = manager.Create("Reserved", 4, 10);
        var availableRoom = manager.Create("Available", 4, 10);
        var catalog = new BattleFieldRoomCatalog();
        catalog.AddRoom(reservedRoom);
        catalog.AddRoom(availableRoom);

        var targetRequest = new BattleFieldRoomJoinRequest(10, 100, reservedRoom.RoomUid, 0, RequiredSlots: 2);
        Assert(catalog.TrySelectRoom(
            targetRequest,
            out var targetSelected,
            reservedUserCount: room => room.RoomUid == reservedRoom.RoomUid ? 3 : 0,
            randomSelector: static rooms => rooms[0]));
        Assert(targetSelected!.RoomUid == availableRoom.RoomUid);

        var randomRequest = new BattleFieldRoomJoinRequest(10, 100, 0, 0, RequiredSlots: 2);
        Assert(catalog.TrySelectRoom(
            randomRequest,
            out var randomSelected,
            reservedUserCount: room => room.RoomUid == reservedRoom.RoomUid ? 3 : 0,
            randomSelector: static rooms => rooms[0]));
        Assert(randomSelected!.RoomUid == availableRoom.RoomUid);
    }

    private static void TestNoCandidateIsNotAnError()
    {
        var manager = new BattleFieldRoomManager();
        var room = manager.Create("TooSmall", 1, 10);

        var catalog = new BattleFieldRoomCatalog();
        catalog.AddRoom(room);

        var request = new BattleFieldRoomJoinRequest(10, 100, 0, 0, RequiredSlots: 2);
        Assert(catalog.TrySelectRoom(request, out var selected));
        Assert(selected is null);
    }

    private static void TestCapacityAndAlreadyJoined()
    {
        var manager = new BattleFieldRoomManager();
        var room = manager.Create("Existing", 2, 10);
        Assert(room.OpenSlot(0));
        Assert(room.OpenSlot(1));
        Assert(manager.AddUnit(room.RoomUid, 100, out _));

        var catalog = new BattleFieldRoomCatalog();
        catalog.AddRoom(room);

        var existingUserRequest = new BattleFieldRoomJoinRequest(10, 100, room.RoomUid, 0, RequiredSlots: 1);
        Assert(catalog.TrySelectRoom(existingUserRequest, out var existingSelected, unitAlreadyJoined: static _ => true));
        Assert(existingSelected!.RoomUid == room.RoomUid);

        var newUserRequest = new BattleFieldRoomJoinRequest(10, 200, room.RoomUid, 0, RequiredSlots: 2);
        Assert(catalog.TrySelectRoom(newUserRequest, out var newSelected));
        Assert(newSelected is null);
    }

    private static void Assert(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("BattleField room catalog compatibility assertion failed.");
    }
}
