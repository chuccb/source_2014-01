namespace KncWX2Server.CSharp14.Protocol;

public sealed class KQuestUpdate
{
    public int QuestId { get; set; }
    public List<byte> ClearData { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.QuestId);
            new NativeStlSerializer(ser).PutVector(value.ClearData, static (s, item) => s.Put(item));
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KQuestUpdate value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int questId) ||
                !new NativeStlSerializer(ser).TryGetVector(out List<byte> clearData,
                    static s => s.TryGet(out byte item) ? (true, item) : (false, 0)))
                return (false, x);
            x.QuestId = questId;
            x.ClearData.AddRange(clearData);
            return (true, x);
        });
    }
}

public sealed class KDenyOptions
{
    public sbyte DenyFriendShip { get; set; }
    public sbyte DenyInviteGuild { get; set; }
    public sbyte DenyParty { get; set; }
    public sbyte DenyPersonalTrade { get; set; }
    public sbyte DenyRequestCouple { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options) =>
        new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.DenyFriendShip);
            ser.Put(value.DenyInviteGuild);
            ser.Put(value.DenyParty);
            ser.Put(value.DenyPersonalTrade);
            if (options.RelationshipSystem)
                ser.Put(value.DenyRequestCouple);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KDenyOptions value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, x) =>
        {
            if (!ser.TryGet(out sbyte friend) || !ser.TryGet(out sbyte guild) ||
                !ser.TryGet(out sbyte party) || !ser.TryGet(out sbyte trade))
                return (false, x);
            x.DenyFriendShip = friend;
            x.DenyInviteGuild = guild;
            x.DenyParty = party;
            x.DenyPersonalTrade = trade;
            if (options.RelationshipSystem && !ser.TryGet(out x.DenyRequestCouple))
                return (false, x);
            return (true, x);
        });
    }
}

public sealed class KChatBlackListUnit
{
    public long UnitUid { get; set; }
    public string NickName { get; set; } = string.Empty;

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.UnitUid);
            ser.PutWString(value.NickName);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KChatBlackListUnit value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out long uid) || !ser.TryGetWString(out var name)) return (false, x);
            x.UnitUid = uid;
            x.NickName = name;
            return (true, x);
        });
    }
}

public sealed class KRankerInfo
{
    public long UnitUid { get; set; }
    public string NickName { get; set; } = string.Empty;

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.UnitUid); ser.PutWString(value.NickName); return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KRankerInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out long uid) || !ser.TryGetWString(out var name)) return (false, x);
            x.UnitUid = uid; x.NickName = name; return (true, x);
        });
    }
}

public sealed class KMessengerInfo
{
    public SortedDictionary<long, KFriendInfo> FriendInfo { get; } = [];
    public SortedDictionary<sbyte, string> Group { get; } = [];
    public List<KFriendMessageInfo> FriendMessages { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            var stl = new NativeStlSerializer(ser);
            stl.PutMap(value.FriendInfo, static (s, key) => s.Put(key), static (s, item) => item.Serialize(s));
            stl.PutMap(value.Group, static (s, key) => s.Put(key), static (s, item) => s.PutWString(item));
            stl.PutVector(value.FriendMessages, static (s, item) => item.Serialize(s));
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KMessengerInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            var stl = new NativeStlSerializer(ser);
            if (!stl.TryGetMap(out SortedDictionary<long, KFriendInfo> friends,
                    static s => s.TryGet(out long key) ? (true, key) : (false, 0),
                    static s => KFriendInfo.TryDeserialize(s, out var item) ? (true, item) : (false, new KFriendInfo())) ||
                !stl.TryGetMap(out SortedDictionary<sbyte, string> groups,
                    static s => s.TryGet(out sbyte key) ? (true, key) : (false, (sbyte)0),
                    static s => s.TryGetWString(out var item) ? (true, item) : (false, string.Empty)) ||
                !stl.TryGetVector(out List<KFriendMessageInfo> messages,
                    static s => KFriendMessageInfo.TryDeserialize(s, out var item) ? (true, item) : (false, new KFriendMessageInfo())))
                return (false, x);

            x.FriendInfo.Clear();
            foreach (var pair in friends) x.FriendInfo.Add(pair.Key, pair.Value);
            x.Group.Clear();
            foreach (var pair in groups) x.Group.Add(pair.Key, pair.Value);
            x.FriendMessages.Clear();
            x.FriendMessages.AddRange(messages);
            return (true, x);
        });
    }
}
