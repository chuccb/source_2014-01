namespace KncWX2Server.CSharp14.Protocol;

public sealed class KRoomInfo
{
    public sbyte RoomType { get; set; }
    public long RoomUid { get; set; }
    public uint RoomListId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public sbyte RoomState { get; set; }
    public bool Public { get; set; }
    public bool TeamBalance { get; set; }
    public string Password { get; set; } = string.Empty;
    public sbyte MaxSlot { get; set; }
    public sbyte JoinSlot { get; set; }
    public string UdpRelayIp { get; set; } = string.Empty;
    public ushort UdpRelayPort { get; set; }
    public bool StartedByAutoParty { get; set; }
    public sbyte PvpGameType { get; set; }
    public bool IsItemMode { get; set; }
    public int PvpChannelClass { get; set; }
    public sbyte WinKillNum { get; set; }
    public bool CanIntrude { get; set; }
    public float PlayTime { get; set; }
    public short WorldId { get; set; }
    public short ShowPvpMapWorldId { get; set; }
    public sbyte DifficultyLevel { get; set; }
    public int DungeonId { get; set; }
    public sbyte GetItemType { get; set; }
    public sbyte DungeonMode { get; set; }
    public long PartyUid { get; set; }
    public int BuffType { get; set; }
    public int BattleFieldId { get; set; }
    public SortedDictionary<short, int> AllPlayersSelectedMap { get; } = [];
    public bool DefenceDungeonOpen { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.RoomType); ser.Put(value.RoomUid); ser.Put(value.RoomListId); ser.PutWString(value.RoomName);
            ser.Put(value.RoomState); ser.Put(value.Public); ser.Put(value.TeamBalance); ser.PutWString(value.Password);
            ser.Put(value.MaxSlot); ser.Put(value.JoinSlot); ser.PutWString(value.UdpRelayIp); ser.Put(value.UdpRelayPort);
            if (options.BattleFieldSystem) ser.Put(value.StartedByAutoParty);
            ser.Put(value.PvpGameType);
            if (options.DungeonItem) ser.Put(value.IsItemMode);
            ser.Put(value.PvpChannelClass); ser.Put(value.WinKillNum); ser.Put(value.CanIntrude); ser.Put(value.PlayTime);
            ser.Put(value.WorldId); ser.Put(value.ShowPvpMapWorldId); ser.Put(value.DifficultyLevel); ser.Put(value.DungeonId);
            ser.Put(value.GetItemType); ser.Put(value.DungeonMode); ser.Put(value.PartyUid);
            if (options.CoexistenceFestivalRoomBuff) ser.Put(value.BuffType);
            if (options.BattleFieldSystem) ser.Put(value.BattleFieldId);
            if (options.PvpRematch)
                new NativeStlSerializer(ser).PutMap(value.AllPlayersSelectedMap, static (s, k) => s.Put(k), static (s, v) => s.Put(v));
            if (options.NewDefenceDungeon) ser.Put(value.DefenceDungeonOpen);
            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KRoomInfo value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            if (!ser.TryGet(out sbyte roomType) || !ser.TryGet(out long roomUid) || !ser.TryGet(out uint roomListId) ||
                !ser.TryGetWString(out var roomName) || !ser.TryGet(out sbyte roomState) || !ser.TryGet(out bool isPublic) ||
                !ser.TryGet(out bool teamBalance) || !ser.TryGetWString(out var password) || !ser.TryGet(out sbyte maxSlot) ||
                !ser.TryGet(out sbyte joinSlot) || !ser.TryGetWString(out var relayIp) || !ser.TryGet(out ushort relayPort))
                return (false, existing);

            bool startedByAutoParty = false;
            if (options.BattleFieldSystem && !ser.TryGet(out startedByAutoParty)) return (false, existing);
            if (!ser.TryGet(out sbyte pvpGameType)) return (false, existing);
            bool itemMode = false;
            if (options.DungeonItem && !ser.TryGet(out itemMode)) return (false, existing);
            if (!ser.TryGet(out int pvpChannelClass) || !ser.TryGet(out sbyte winKillNum) || !ser.TryGet(out bool canIntrude) ||
                !ser.TryGet(out float playTime) || !ser.TryGet(out short worldId) || !ser.TryGet(out short showWorldId) ||
                !ser.TryGet(out sbyte difficulty) || !ser.TryGet(out int dungeonId) || !ser.TryGet(out sbyte getItemType) ||
                !ser.TryGet(out sbyte dungeonMode) || !ser.TryGet(out long partyUid)) return (false, existing);

            int buffType = 0;
            if (options.CoexistenceFestivalRoomBuff && !ser.TryGet(out buffType)) return (false, existing);
            int battlefieldId = 0;
            if (options.BattleFieldSystem && !ser.TryGet(out battlefieldId)) return (false, existing);

            SortedDictionary<short, int> selectedMap = [];
            if (options.PvpRematch && !new NativeStlSerializer(ser).TryGetMap(out selectedMap,
                    static s => s.TryGet(out short v) ? (true, v) : (false, (short)0),
                    static s => s.TryGet(out int v) ? (true, v) : (false, 0))) return (false, existing);

            bool defenceOpen = false;
            if (options.NewDefenceDungeon && !ser.TryGet(out defenceOpen)) return (false, existing);

            existing.RoomType = roomType; existing.RoomUid = roomUid; existing.RoomListId = roomListId;
            existing.RoomName = roomName; existing.RoomState = roomState; existing.Public = isPublic; existing.TeamBalance = teamBalance;
            existing.Password = password; existing.MaxSlot = maxSlot; existing.JoinSlot = joinSlot; existing.UdpRelayIp = relayIp;
            existing.UdpRelayPort = relayPort; existing.StartedByAutoParty = startedByAutoParty; existing.PvpGameType = pvpGameType;
            existing.IsItemMode = itemMode; existing.PvpChannelClass = pvpChannelClass; existing.WinKillNum = winKillNum;
            existing.CanIntrude = canIntrude; existing.PlayTime = playTime; existing.WorldId = worldId; existing.ShowPvpMapWorldId = showWorldId;
            existing.DifficultyLevel = difficulty; existing.DungeonId = dungeonId; existing.GetItemType = getItemType; existing.DungeonMode = dungeonMode;
            existing.PartyUid = partyUid; existing.BuffType = buffType; existing.BattleFieldId = battlefieldId;
            existing.AllPlayersSelectedMap.Clear(); foreach (var pair in selectedMap) existing.AllPlayersSelectedMap[pair.Key] = pair.Value;
            existing.DefenceDungeonOpen = defenceOpen;
            return (true, existing);
        });
    }
}
