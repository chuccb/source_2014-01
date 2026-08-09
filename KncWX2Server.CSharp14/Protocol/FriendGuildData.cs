namespace KncWX2Server.CSharp14.Protocol;

public sealed class KFriendInfo
{
    public long UnitUid { get; set; }
    public string NickName { get; set; } = string.Empty;
    public sbyte GroupId { get; set; }
    public sbyte Status { get; set; }
    public sbyte Position { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.UnitUid); ser.PutWString(value.NickName); ser.Put(value.GroupId);
            ser.Put(value.Status); ser.Put(value.Position); return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KFriendInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out long uid) || !ser.TryGetWString(out var name) ||
                !ser.TryGet(out sbyte group) || !ser.TryGet(out sbyte status) || !ser.TryGet(out sbyte position))
                return (false, x);
            x.UnitUid = uid; x.NickName = name; x.GroupId = group; x.Status = status; x.Position = position;
            return (true, x);
        });
    }
}

public sealed class KFriendMessageInfo
{
    public long UnitUid { get; set; }
    public string NickName { get; set; } = string.Empty;
    public sbyte MessageType { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RegDate { get; set; } = string.Empty;

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.UnitUid); ser.PutWString(value.NickName); ser.Put(value.MessageType);
            ser.PutWString(value.Message); ser.PutWString(value.RegDate); return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KFriendMessageInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out long uid) || !ser.TryGetWString(out var name) || !ser.TryGet(out sbyte type) ||
                !ser.TryGetWString(out var message) || !ser.TryGetWString(out var date))
                return (false, x);
            x.UnitUid = uid; x.NickName = name; x.MessageType = type; x.Message = message; x.RegDate = date;
            return (true, x);
        });
    }
}

public sealed class KUserGuildInfo
{
    public int GuildUid { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public byte MembershipGrade { get; set; }
    public int HonorPoint { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.GuildUid); ser.PutWString(value.GuildName); ser.Put(value.MembershipGrade);
            ser.Put(value.HonorPoint); return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KUserGuildInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int uid) || !ser.TryGetWString(out var name) ||
                !ser.TryGet(out byte grade) || !ser.TryGet(out int honor)) return (false, x);
            x.GuildUid = uid; x.GuildName = name; x.MembershipGrade = grade; x.HonorPoint = honor;
            return (true, x);
        });
    }
}
