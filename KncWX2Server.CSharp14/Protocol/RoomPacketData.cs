namespace KncWX2Server.CSharp14.Protocol;

public sealed class KNetAddress
{
    public string Ip { get; set; } = string.Empty;
    public ushort Port { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.PutWString(value.Ip);
            ser.Put(value.Port);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KNetAddress value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGetWString(out var ip) || !ser.TryGet(out ushort port)) return (false, x);
            x.Ip = ip;
            x.Port = port;
            return (true, x);
        });
    }
}

public sealed class KPacketOk
{
    public int Ok { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.Ok);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KPacketOk value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int ok)) return (false, x);
            x.Ok = ok;
            return (true, x);
        });
    }
}

public sealed class KRoomSlotInfo
{
    public sbyte Index { get; set; }
    public sbyte SlotState { get; set; }
    public bool Host { get; set; }
    public bool Ready { get; set; }
    public bool PitIn { get; set; }
    public bool Trade { get; set; }
    public int TeamNum { get; set; }
    public KRoomUserInfo RoomUserInfo { get; set; } = new();
    public bool IsBoss { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options) =>
        new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.Index);
            ser.Put(value.SlotState);
            ser.Put(value.Host);
            ser.Put(value.Ready);
            ser.Put(value.PitIn);
            ser.Put(value.Trade);
            ser.Put(value.TeamNum);
            value.RoomUserInfo.Serialize(ser, options);
            if (options.PvpBossCombatTest)
                ser.Put(value.IsBoss);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KRoomSlotInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, x) =>
        {
            if (!ser.TryGet(out sbyte index) || !ser.TryGet(out sbyte slotState) ||
                !ser.TryGet(out bool host) || !ser.TryGet(out bool ready) || !ser.TryGet(out bool pitIn) ||
                !ser.TryGet(out bool trade) || !ser.TryGet(out int teamNum) ||
                !KRoomUserInfo.TryDeserialize(ser, options, out var user))
                return (false, x);
            x.Index = index;
            x.SlotState = slotState;
            x.Host = host;
            x.Ready = ready;
            x.PitIn = pitIn;
            x.Trade = trade;
            x.TeamNum = teamNum;
            x.RoomUserInfo = user;
            if (options.PvpBossCombatTest)
            {
                if (!ser.TryGet(out bool isBoss))
                    return (false, x);
                x.IsBoss = isBoss;
            }
            return (true, x);
        });
    }
}

public sealed class KOpenRoomRequest
{
    public bool QuickJoin { get; set; }
    public KRoomInfo RoomInfo { get; set; } = new();
    public KRoomUserInfo RoomUserInfo { get; set; } = new();
    public List<long> StudentUnitUid { get; } = [];
    public string ChannelIp { get; set; } = string.Empty;
    public int CurExp { get; set; }
    public int CurEd { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options) =>
        new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.QuickJoin);
            value.RoomInfo.Serialize(ser, options);
            value.RoomUserInfo.Serialize(ser, options);
            new NativeStlSerializer(ser).PutVector(value.StudentUnitUid, static (s, uid) => s.Put(uid));
            if (options.AddDungeonLogColumnNum2)
                ser.PutWString(value.ChannelIp);
            if (options.BattleFieldSystem)
            {
                ser.Put(value.CurExp);
                ser.Put(value.CurEd);
            }
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KOpenRoomRequest value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, x) =>
        {
            if (!ser.TryGet(out bool quickJoin) || !KRoomInfo.TryDeserialize(ser, options, out var room) ||
                !KRoomUserInfo.TryDeserialize(ser, options, out var user) ||
                !new NativeStlSerializer(ser).TryGetVector(out List<long> students,
                    static s => s.TryGet(out long uid) ? (true, uid) : (false, 0)))
                return (false, x);
            x.QuickJoin = quickJoin;
            x.RoomInfo = room;
            x.RoomUserInfo = user;
            x.StudentUnitUid.Clear();
            x.StudentUnitUid.AddRange(students);
            if (options.AddDungeonLogColumnNum2)
            {
                if (!ser.TryGetWString(out var channelIp))
                    return (false, x);
                x.ChannelIp = channelIp;
            }
            if (options.BattleFieldSystem)
            {
                if (!ser.TryGet(out int curExp) || !ser.TryGet(out int curEd))
                    return (false, x);
                x.CurExp = curExp;
                x.CurEd = curEd;
            }
            return (true, x);
        });
    }
}

public sealed class KEcnVerifyServerConnectAck
{
    public int Ok { get; set; }
    public long Uid { get; set; }
    public int DbRegServerGroupId { get; set; }
    public int LocalServerGroupId { get; set; }
    public short GroupId { get; set; }
    public long ServerUid { get; set; }
    public int ServerType { get; set; }
    public string Name { get; set; } = string.Empty;
    public short MaxNum { get; set; }
    public KNetAddress Address { get; set; } = new();
    public int Version { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options) =>
        new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.Ok);
            ser.Put(value.Uid);
            if (options.ServerIntegration)
            {
                ser.Put(value.DbRegServerGroupId);
                ser.Put(value.LocalServerGroupId);
            }
            else
                ser.Put(value.GroupId);
            ser.Put(value.ServerUid);
            if (options.FromChannelToLoginProxy)
                ser.Put(value.ServerType);
            ser.PutWString(value.Name);
            ser.Put(value.MaxNum);
            value.Address.Serialize(ser);
            ser.Put(value.Version);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KEcnVerifyServerConnectAck value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, x) =>
        {
            if (!ser.TryGet(out int ok) || !ser.TryGet(out long uid)) return (false, x);
            x.Ok = ok;
            x.Uid = uid;
            if (options.ServerIntegration)
            {
                if (!ser.TryGet(out int dbRegServerGroupId) || !ser.TryGet(out int localServerGroupId)) return (false, x);
                x.DbRegServerGroupId = dbRegServerGroupId;
                x.LocalServerGroupId = localServerGroupId;
            }
            else
            {
                if (!ser.TryGet(out short groupId)) return (false, x);
                x.GroupId = groupId;
            }
            if (!ser.TryGet(out long serverUid)) return (false, x);
            x.ServerUid = serverUid;
            if (options.FromChannelToLoginProxy)
            {
                if (!ser.TryGet(out int serverType)) return (false, x);
                x.ServerType = serverType;
            }
            if (!ser.TryGetWString(out var name) || !ser.TryGet(out short maxNum) ||
                !KNetAddress.TryDeserialize(ser, out var address) || !ser.TryGet(out int version))
                return (false, x);
            x.Name = name;
            x.MaxNum = maxNum;
            x.Address = address;
            x.Version = version;
            return (true, x);
        });
    }
}
