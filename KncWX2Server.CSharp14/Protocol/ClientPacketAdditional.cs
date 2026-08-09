namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Additional packet slice verified against ClientPacket.h/.cpp.</summary>
public static class ClientPacketAdditional
{
    public sealed class KEGS_CREATE_UNIT_REQ
    {
        public string NickName { get; set; } = string.Empty;
        public int Class { get; set; }

        public bool Serialize(NativePrimitiveSerializer serializer) =>
            NativePacketSerializer.Put(serializer, this, static (ser, value) =>
            {
                ser.PutWString(value.NickName);
                ser.Put(value.Class);
                return true;
            });

        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_CREATE_UNIT_REQ value)
        {
            value = new();
            return NativePacketSerializer.TryGet(serializer, out value,
                static (ser, packet) =>
                {
                    if (!ser.TryGetWString(out var nickName) || !ser.TryGet(out int @class))
                        return false;
                    packet.NickName = nickName;
                    packet.Class = @class;
                    return true;
                },
                static () => new());
        }
    }

    public sealed class KEGS_SELECT_SERVER_SET_REQ
    {
        public int ServerSetId { get; set; }

        public bool Serialize(NativePrimitiveSerializer serializer) =>
            NativePacketSerializer.Put(serializer, this,
                static (ser, value) =>
                {
                    ser.Put(value.ServerSetId);
                    return true;
                });

        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_SELECT_SERVER_SET_REQ value)
        {
            value = new();
            return NativePacketSerializer.TryGet(serializer, out value,
                static (ser, packet) =>
                {
                    if (!ser.TryGet(out int serverSetId))
                        return false;
                    packet.ServerSetId = serverSetId;
                    return true;
                },
                static () => new());
        }
    }

    public sealed class KEGS_GET_SERVER_SET_DATA_ACK
    {
        public int Ok { get; set; }
        public List<KServerSetData> ServerSetList { get; } = [];

        public bool Serialize(NativePrimitiveSerializer serializer) =>
            NativePacketSerializer.Put(serializer, this, static (ser, value) =>
            {
                ser.Put(value.Ok);
                new NativeStlSerializer(ser).PutVector(
                    value.ServerSetList,
                    static (inner, item) => item.Serialize(inner));
                return true;
            });

        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_GET_SERVER_SET_DATA_ACK value)
        {
            value = new();
            return NativePacketSerializer.TryGet(serializer, out value,
                static (ser, packet) =>
                {
                    if (!ser.TryGet(out int ok))
                        return false;

                    var stl = new NativeStlSerializer(ser);
                    if (!stl.TryGetVector(out List<KServerSetData> list,
                        static inner => KServerSetData.TryDeserialize(inner, out var item)
                            ? (true, item)
                            : (false, null!)))
                        return false;

                    packet.Ok = ok;
                    packet.ServerSetList.Clear();
                    packet.ServerSetList.AddRange(list);
                    return true;
                },
                static () => new());
        }
    }
}
