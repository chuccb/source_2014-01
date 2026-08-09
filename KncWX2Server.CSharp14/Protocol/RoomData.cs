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
            ser.Put(value.RoomType);
            ser.Put(value.RoomUid);
            ser.Put(value.RoomListId);
            ser.PutWString(value.RoomName);
            ser.Put(value.RoomState);
            ser.Put(value.Public);
            ser.Put(value.TeamBalance);
            ser.PutWString(value.Password);
            ser.Put(value.MaxSlot);
            ser.Put(value.JoinSlot);
            ser.PutWString(value.UdpRelayIp);
            ser.Put(value.UdpRelayPort);

            if (options.BattleFieldSystem)
            {
                ser.Put(value.StartedByAutoParty);
            }

            ser.Put(value.PvpGameType);

            if (options.DungeonItem)
            {
                ser.Put(value.IsItemMode);
            }

            ser.Put(value.PvpChannelClass);
            ser.Put(value.WinKillNum);
            ser.Put(value.CanIntrude);
            ser.Put(value.PlayTime);
            ser.Put(value.WorldId);
            ser.Put(value.ShowPvpMapWorldId);
            ser.Put(value.DifficultyLevel);
            ser.Put(value.DungeonId);
            ser.Put(value.GetItemType);
            ser.Put(value.DungeonMode);
            ser.Put(value.PartyUid);

            if (options.CoexistenceFestivalRoomBuff)
            {
                ser.Put(value.BuffType);
            }

            if (options.BattleFieldSystem)
            {
                ser.Put(value.BattleFieldId);
            }

            if (options.PvpRematch)
            {
                new NativeStlSerializer(ser).PutMap(
                    value.AllPlayersSelectedMap,
                    static (s, key) => s.Put(key),
                    static (s, item) => s.Put(item));
            }

            if (options.NewDefenceDungeon)
            {
                ser.Put(value.DefenceDungeonOpen);
            }

            return true;
        });
    }

    public static bool TryDeserialize(
        NativePrimitiveSerializer serializer,
        ProtocolOptions options,
        out KRoomInfo value)
    {
        ArgumentNullException.ThrowIfNull(options);

        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            if (!TryReadRequiredFields(ser, out var fields))
            {
                return (false, existing);
            }

            var startedByAutoParty = false;
            if (options.BattleFieldSystem && !ser.TryGet(out startedByAutoParty))
            {
                return (false, existing);
            }

            if (!ser.TryGet(out sbyte pvpGameType))
            {
                return (false, existing);
            }

            var itemMode = false;
            if (options.DungeonItem && !ser.TryGet(out itemMode))
            {
                return (false, existing);
            }

            if (!TryReadGameplayFields(ser, out var gameplayFields))
            {
                return (false, existing);
            }

            var buffType = 0;
            if (options.CoexistenceFestivalRoomBuff && !ser.TryGet(out buffType))
            {
                return (false, existing);
            }

            var battlefieldId = 0;
            if (options.BattleFieldSystem && !ser.TryGet(out battlefieldId))
            {
                return (false, existing);
            }

            SortedDictionary<short, int> selectedMap = [];
            if (options.PvpRematch && !new NativeStlSerializer(ser).TryGetMap(
                    out selectedMap,
                    GetInt16,
                    GetInt32))
            {
                return (false, existing);
            }

            var defenceOpen = false;
            if (options.NewDefenceDungeon && !ser.TryGet(out defenceOpen))
            {
                return (false, existing);
            }

            existing.RoomType = fields.RoomType;
            existing.RoomUid = fields.RoomUid;
            existing.RoomListId = fields.RoomListId;
            existing.RoomName = fields.RoomName;
            existing.RoomState = fields.RoomState;
            existing.Public = fields.Public;
            existing.TeamBalance = fields.TeamBalance;
            existing.Password = fields.Password;
            existing.MaxSlot = fields.MaxSlot;
            existing.JoinSlot = fields.JoinSlot;
            existing.UdpRelayIp = fields.RelayIp;
            existing.UdpRelayPort = fields.RelayPort;
            existing.StartedByAutoParty = startedByAutoParty;
            existing.PvpGameType = pvpGameType;
            existing.IsItemMode = itemMode;
            existing.PvpChannelClass = gameplayFields.PvpChannelClass;
            existing.WinKillNum = gameplayFields.WinKillNum;
            existing.CanIntrude = gameplayFields.CanIntrude;
            existing.PlayTime = gameplayFields.PlayTime;
            existing.WorldId = gameplayFields.WorldId;
            existing.ShowPvpMapWorldId = gameplayFields.ShowWorldId;
            existing.DifficultyLevel = gameplayFields.Difficulty;
            existing.DungeonId = gameplayFields.DungeonId;
            existing.GetItemType = gameplayFields.GetItemType;
            existing.DungeonMode = gameplayFields.DungeonMode;
            existing.PartyUid = gameplayFields.PartyUid;
            existing.BuffType = buffType;
            existing.BattleFieldId = battlefieldId;
            existing.AllPlayersSelectedMap.Clear();

            foreach (var pair in selectedMap)
            {
                existing.AllPlayersSelectedMap[pair.Key] = pair.Value;
            }

            existing.DefenceDungeonOpen = defenceOpen;
            return (true, existing);
        });
    }

    private static bool TryReadRequiredFields(
        NativePrimitiveSerializer serializer,
        out RequiredFields fields)
    {
        if (!serializer.TryGet(out sbyte roomType) ||
            !serializer.TryGet(out long roomUid) ||
            !serializer.TryGet(out uint roomListId) ||
            !serializer.TryGetWString(out var roomName) ||
            !serializer.TryGet(out sbyte roomState) ||
            !serializer.TryGet(out bool isPublic) ||
            !serializer.TryGet(out bool teamBalance) ||
            !serializer.TryGetWString(out var password) ||
            !serializer.TryGet(out sbyte maxSlot) ||
            !serializer.TryGet(out sbyte joinSlot) ||
            !serializer.TryGetWString(out var relayIp) ||
            !serializer.TryGet(out ushort relayPort))
        {
            fields = default;
            return false;
        }

        fields = new(
            roomType,
            roomUid,
            roomListId,
            roomName,
            roomState,
            isPublic,
            teamBalance,
            password,
            maxSlot,
            joinSlot,
            relayIp,
            relayPort);
        return true;
    }

    private static bool TryReadGameplayFields(
        NativePrimitiveSerializer serializer,
        out GameplayFields fields)
    {
        if (!serializer.TryGet(out int pvpChannelClass) ||
            !serializer.TryGet(out sbyte winKillNum) ||
            !serializer.TryGet(out bool canIntrude) ||
            !serializer.TryGet(out float playTime) ||
            !serializer.TryGet(out short worldId) ||
            !serializer.TryGet(out short showWorldId) ||
            !serializer.TryGet(out sbyte difficulty) ||
            !serializer.TryGet(out int dungeonId) ||
            !serializer.TryGet(out sbyte getItemType) ||
            !serializer.TryGet(out sbyte dungeonMode) ||
            !serializer.TryGet(out long partyUid))
        {
            fields = default;
            return false;
        }

        fields = new(
            pvpChannelClass,
            winKillNum,
            canIntrude,
            playTime,
            worldId,
            showWorldId,
            difficulty,
            dungeonId,
            getItemType,
            dungeonMode,
            partyUid);
        return true;
    }

    private static (bool Ok, short Value) GetInt16(NativePrimitiveSerializer serializer)
        => serializer.TryGet(out short value) ? (true, value) : (false, default);

    private static (bool Ok, int Value) GetInt32(NativePrimitiveSerializer serializer)
        => serializer.TryGet(out int value) ? (true, value) : (false, default);

    private readonly record struct RequiredFields(
        sbyte RoomType,
        long RoomUid,
        uint RoomListId,
        string RoomName,
        sbyte RoomState,
        bool Public,
        bool TeamBalance,
        string Password,
        sbyte MaxSlot,
        sbyte JoinSlot,
        string RelayIp,
        ushort RelayPort);

    private readonly record struct GameplayFields(
        int PvpChannelClass,
        sbyte WinKillNum,
        bool CanIntrude,
        float PlayTime,
        short WorldId,
        short ShowWorldId,
        sbyte Difficulty,
        int DungeonId,
        sbyte GetItemType,
        sbyte DungeonMode,
        long PartyUid);
}
