namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Additional packet slice verified against native ClientPacket.h/.cpp.</summary>
public static class ClientPacketAdditional
{
    public sealed class KEGS_CREATE_UNIT_REQ
    {
        public string NickName { get; set; } = string.Empty;
        public int Class { get; set; }
        public bool Serialize(NativePrimitiveSerializer serializer) => NativePacketSerializer.Put(serializer, this, static (ser, value) => ser.PutWString(value.NickName) && ser.Put(value.Class));
        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_CREATE_UNIT_REQ value) => NativePacketSerializer.TryGet(serializer, out value, static (ser, packet) => { if (!ser.TryGetWString(out var nickName) || !ser.TryGet(out int @class)) return false; packet.NickName = nickName; packet.Class = @class; return true; }, static () => new());
    }

    public sealed class KEGS_SELECT_SERVER_SET_REQ
    {
        public int ServerSetId { get; set; }
        public bool Serialize(NativePrimitiveSerializer serializer) => NativePacketSerializer.Put(serializer, this, static (ser, value) => ser.Put(value.ServerSetId));
        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_SELECT_SERVER_SET_REQ value) => NativePacketSerializer.TryGet(serializer, out value, static (ser, packet) => { if (!ser.TryGet(out int id)) return false; packet.ServerSetId = id; return true; }, static () => new());
    }

    public sealed class KEGS_GET_SERVER_SET_DATA_ACK
    {
        public int Ok { get; set; }
        public List<KServerSetData> ServerSetList { get; } = [];
        public bool Serialize(NativePrimitiveSerializer serializer) => NativePacketSerializer.Put(serializer, this, static (ser, value) => ser.Put(value.Ok) && new NativeStlSerializer(ser).PutVector(value.ServerSetList, static (inner, item) => item.Serialize(inner)));
        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_GET_SERVER_SET_DATA_ACK value) => NativePacketSerializer.TryGet(serializer, out value, static (ser, packet) => { if (!ser.TryGet(out int ok)) return false; var stl = new NativeStlSerializer(ser); if (!stl.TryGetVector(out List<KServerSetData> list, static inner => KServerSetData.TryDeserialize(inner, out var item) ? (true, item) : (false, null!))) return false; packet.Ok = ok; packet.ServerSetList.Clear(); packet.ServerSetList.AddRange(list); return true; }, static () => new());
    }

    public sealed class KEGS_STATE_CHANGE_SERVER_SELECT_ACK
    {
        public int Ok { get; set; }
        public bool Serialize(NativePrimitiveSerializer serializer) => NativePacketSerializer.Put(serializer, this, static (ser, value) => ser.Put(value.Ok));
        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_STATE_CHANGE_SERVER_SELECT_ACK value) => NativePacketSerializer.TryGet(serializer, out value, static (ser, packet) => { if (!ser.TryGet(out int ok)) return false; packet.Ok = ok; return true; }, static () => new());
    }

    public sealed class KEGS_CHECK_CHANNEL_CHANGE_REQ
    {
        public int ChannelId { get; set; }
        public bool Serialize(NativePrimitiveSerializer serializer) => NativePacketSerializer.Put(serializer, this, static (ser, value) => ser.Put(value.ChannelId));
        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_CHECK_CHANNEL_CHANGE_REQ value) => NativePacketSerializer.TryGet(serializer, out value, static (ser, packet) => { if (!ser.TryGet(out int id)) return false; packet.ChannelId = id; return true; }, static () => new());
    }

    public sealed class KStateChangeReq
    {
        public int MapId { get; set; }
        public bool Serialize(NativePrimitiveSerializer serializer) => NativePacketSerializer.Put(serializer, this, static (ser, value) => ser.Put(value.MapId));
        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KStateChangeReq value) => NativePacketSerializer.TryGet(serializer, out value, static (ser, packet) => { if (!ser.TryGet(out int id)) return false; packet.MapId = id; return true; }, static () => new());
    }

    public sealed class KStateChangeAck
    {
        public int Ok { get; set; }
        public int MapId { get; set; }
        public bool Serialize(NativePrimitiveSerializer serializer) => NativePacketSerializer.Put(serializer, this, static (ser, value) => ser.Put(value.Ok) && ser.Put(value.MapId));
        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KStateChangeAck value) => NativePacketSerializer.TryGet(serializer, out value, static (ser, packet) => { if (!ser.TryGet(out int ok) || !ser.TryGet(out int mapId)) return false; packet.Ok = ok; packet.MapId = mapId; return true; }, static () => new());
    }

    public sealed class KEGS_ENTER_NPC_HOUSE_REQ
    {
        public int NpcId { get; set; }
        public bool Serialize(NativePrimitiveSerializer serializer) => NativePacketSerializer.Put(serializer, this, static (ser, value) => ser.Put(value.NpcId));
        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_ENTER_NPC_HOUSE_REQ value) => NativePacketSerializer.TryGet(serializer, out value, static (ser, packet) => { if (!ser.TryGet(out int id)) return false; packet.NpcId = id; return true; }, static () => new());
    }

    public sealed class KEGS_ROOM_LIST_REQ
    {
        public int ViewPage { get; set; }
        public sbyte PvpGameType { get; set; }
        public bool Serialize(NativePrimitiveSerializer serializer) => NativePacketSerializer.Put(serializer, this, static (ser, value) => ser.Put(value.ViewPage) && ser.Put(value.PvpGameType));
        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_ROOM_LIST_REQ value) => NativePacketSerializer.TryGet(serializer, out value, static (ser, packet) => { if (!ser.TryGet(out int page) || !ser.TryGet(out sbyte gameType)) return false; packet.ViewPage = page; packet.PvpGameType = gameType; return true; }, static () => new());
    }
}
