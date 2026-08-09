namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// First packet slice ported directly from Common/ClientPacket.h.
/// The native DECL_PACKET(id) macro creates K&lt;id&gt;; member order here follows
/// the native struct declaration exactly. Conditional native fields are not
/// silently guessed when their build symbol is unavailable in this repository.
/// </summary>
public static class ClientPacket
{
    public sealed class KEGS_NEW_USER_JOIN_REQ
    {
        public string Id { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public byte GuestUser { get; set; }

        public bool Serialize(NativePrimitiveSerializer serializer) =>
            NativePacketSerializer.Put(serializer, this, static (ser, value) =>
            {
                ser.PutWString(value.Id);
                ser.PutWString(value.Password);
                ser.PutWString(value.Name);
                ser.Put(value.GuestUser);
                return true;
            });

        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_NEW_USER_JOIN_REQ value)
        {
            value = new();
            return NativePacketSerializer.TryGet(serializer, out value,
                static (ser, packet) =>
                {
                    if (!ser.TryGetWString(out var id) ||
                        !ser.TryGetWString(out var password) ||
                        !ser.TryGetWString(out var name) ||
                        !ser.TryGet(out byte guestUser))
                        return false;

                    packet.Id = id;
                    packet.Password = password;
                    packet.Name = name;
                    packet.GuestUser = guestUser;
                    return true;
                },
                static () => new());
        }
    }

    public sealed class KEGS_NEW_USER_JOIN_ACK
    {
        public int Ok { get; set; }
        public long UserUid { get; set; }

        public bool Serialize(NativePrimitiveSerializer serializer) =>
            NativePacketSerializer.Put(serializer, this, static (ser, value) =>
            {
                ser.Put(value.Ok);
                ser.Put(value.UserUid);
                return true;
            });

        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_NEW_USER_JOIN_ACK value)
        {
            value = new();
            return NativePacketSerializer.TryGet(serializer, out value,
                static (ser, packet) =>
                {
                    if (!ser.TryGet(out int ok) || !ser.TryGet(out long userUid))
                        return false;
                    packet.Ok = ok;
                    packet.UserUid = userUid;
                    return true;
                },
                static () => new());
        }
    }

    public sealed class KEGS_DELETE_UNIT_REQ
    {
        public long UnitUid { get; set; }

        public bool Serialize(NativePrimitiveSerializer serializer) =>
            NativePacketSerializer.Put(serializer, this,
                static (ser, value) =>
                {
                    ser.Put(value.UnitUid);
                    return true;
                });

        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_DELETE_UNIT_REQ value)
        {
            value = new();
            return NativePacketSerializer.TryGet(serializer, out value,
                static (ser, packet) =>
                {
                    if (!ser.TryGet(out long unitUid))
                        return false;
                    packet.UnitUid = unitUid;
                    return true;
                },
                static () => new());
        }
    }

    public sealed class KEGS_GET_MY_INVENTORY_ACK
    {
        public int Ok { get; set; }
        public bool IsRecommend { get; set; }

        public bool Serialize(NativePrimitiveSerializer serializer) =>
            NativePacketSerializer.Put(serializer, this,
                static (ser, value) =>
                {
                    ser.Put(value.Ok);
                    ser.Put(value.IsRecommend);
                    return true;
                });

        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_GET_MY_INVENTORY_ACK value)
        {
            value = new();
            return NativePacketSerializer.TryGet(serializer, out value,
                static (ser, packet) =>
                {
                    if (!ser.TryGet(out int ok) || !ser.TryGet(out bool isRecommend))
                        return false;
                    packet.Ok = ok;
                    packet.IsRecommend = isRecommend;
                    return true;
                },
                static () => new());
        }
    }

    public sealed class KEGS_STATE_CHANGE_SERVER_SELECT_ACK
    {
        public int Ok { get; set; }

        public bool Serialize(NativePrimitiveSerializer serializer) =>
            NativePacketSerializer.Put(serializer, this,
                static (ser, value) =>
                {
                    ser.Put(value.Ok);
                    return true;
                });

        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_STATE_CHANGE_SERVER_SELECT_ACK value)
        {
            value = new();
            return NativePacketSerializer.TryGet(serializer, out value,
                static (ser, packet) =>
                {
                    if (!ser.TryGet(out int ok))
                        return false;
                    packet.Ok = ok;
                    return true;
                },
                static () => new());
        }
    }

    public sealed class KEGS_CHECK_CHANNEL_CHANGE_REQ
    {
        public int ChannelId { get; set; }

        public bool Serialize(NativePrimitiveSerializer serializer) =>
            NativePacketSerializer.Put(serializer, this,
                static (ser, value) =>
                {
                    ser.Put(value.ChannelId);
                    return true;
                });

        public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KEGS_CHECK_CHANNEL_CHANGE_REQ value)
        {
            value = new();
            return NativePacketSerializer.TryGet(serializer, out value,
                static (ser, packet) =>
                {
                    if (!ser.TryGet(out int channelId))
                        return false;
                    packet.ChannelId = channelId;
                    return true;
                },
                static () => new());
        }
    }
}
