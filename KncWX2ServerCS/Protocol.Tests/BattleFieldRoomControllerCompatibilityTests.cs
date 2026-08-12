using KncWX2Server.Runtime.BattleField;

internal static class BattleFieldRoomControllerCompatibilityTests
{
    public static void Run()
    {
        var rooms = new BattleFieldRoomManager();
        var controller = new RoomController(rooms);
        var room = controller.Create("controller", 4, 77);

        Require(room.State == RoomState.Wait, "room manager creates rooms in Wait state");
        Require(controller.Transition(room.RoomUid, RoomState.TimeCount), "Wait -> TimeCount");
        Require(room.State == RoomState.TimeCount, "controller must update room state");
        Require(!controller.Transition(room.RoomUid, RoomState.Init), "invalid TimeCount -> Init must fail");
        Require(room.State == RoomState.TimeCount, "rejected transition must preserve room state");
        Require(controller.Transition(room.RoomUid, RoomState.Loading), "TimeCount -> Loading");
        Require(controller.Transition(room.RoomUid, RoomState.Play), "Loading -> Play");

        Require(controller.Destroy(room.RoomUid), "destroy must remove room");
        Require(!rooms.TryGet(room.RoomUid, out _), "destroy must remove room from manager");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"BattleField RoomController vector failed: {message}");
        }
    }
}
