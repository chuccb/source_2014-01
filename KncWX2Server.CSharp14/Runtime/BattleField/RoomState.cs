namespace KncWX2Server.Runtime.BattleField;

/// <summary>
/// Wire-compatible room states from CX2Room::ROOM_STATE.
/// Keep the numeric values stable: these values are part of the legacy protocol/data model.
/// </summary>
public enum RoomState : byte
{
    Init = 1,
    Closed = 2,
    Wait = 3,
    TimeCount = 4,
    Loading = 5,
    Play = 6,
    Result = 7,
    ReturnToField = 8,
}

/// <summary>
/// Item distribution modes from CX2Room::DUNGEON_GET_ITEM_TYPE.
/// </summary>
public enum DungeonGetItemType : byte
{
    None = 0,
    Random = 1,
    Person = 2,
    End = 3,
}

/// <summary>
/// Compact room information used by room-list responses.
/// This corresponds to CX2Room::RoomSimpleInfo.
/// </summary>
public readonly record struct RoomSimpleInfo(
    long RoomUid,
    string RoomName,
    RoomState State,
    bool IsPublic,
    int MaxSlot,
    int JoinSlot);
