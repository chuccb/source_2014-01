namespace KncWX2Server.Runtime.Center;

/// <summary>Wire-independent snapshot corresponding to KRoom::GetRoomInfo().</summary>
public sealed record RoomInfo(
    int RoomType,
    long RoomUid,
    int RoomListId,
    string RoomName,
    RoomState State,
    bool IsPublic,
    bool TeamBalance,
    string Password,
    int MaxSlot,
    int JoinSlot,
    string UdpRelayIp,
    ushort UdpRelayPort,
    int PvpGameType,
    int WinningKillNum,
    float PlayTime,
    short WorldId,
    short ShowPvpMapWorldId,
    int DifficultyLevel,
    int DungeonId,
    bool CanIntrude,
    int GetItemType,
    int DungeonMode);