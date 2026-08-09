namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Managed counterpart of native DECL_DATA(KWeddingHallInfo).</summary>
/// <remarks>Available when the native SERV_RELATIONSHIP_SYSTEM build feature is enabled.</remarks>
public sealed class KWeddingHallInfo
{
    public int WeddingUid { get; set; }
    public sbyte WeddingHallType { get; set; }
    public sbyte OfficiantNpc { get; set; }
    public long Groom { get; set; }
    public long Bride { get; set; }
    public string WeddingDate { get; set; } = string.Empty;
    public long WeddingDateTicks { get; set; }
    public string WeddingMessage { get; set; } = string.Empty;
    public long RoomUid { get; set; }
    public bool Success { get; set; }
    public bool Delete { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.WeddingUid);
            ser.Put(value.WeddingHallType);
            ser.Put(value.OfficiantNpc);
            ser.Put(value.Groom);
            ser.Put(value.Bride);
            ser.PutWString(value.WeddingDate);
            ser.Put(value.WeddingDateTicks);
            ser.PutWString(value.WeddingMessage);
            ser.Put(value.RoomUid);
            ser.Put(value.Success);
            ser.Put(value.Delete);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KWeddingHallInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int weddingUid) ||
                !ser.TryGet(out sbyte hallType) ||
                !ser.TryGet(out sbyte officiantNpc) ||
                !ser.TryGet(out long groom) ||
                !ser.TryGet(out long bride) ||
                !ser.TryGetWString(out var weddingDate) ||
                !ser.TryGet(out long weddingDateTicks) ||
                !ser.TryGetWString(out var message) ||
                !ser.TryGet(out long roomUid) ||
                !ser.TryGet(out bool success) ||
                !ser.TryGet(out bool delete))
                return (false, existing);
            existing.WeddingUid = weddingUid;
            existing.WeddingHallType = hallType;
            existing.OfficiantNpc = officiantNpc;
            existing.Groom = groom;
            existing.Bride = bride;
            existing.WeddingDate = weddingDate;
            existing.WeddingDateTicks = weddingDateTicks;
            existing.WeddingMessage = message;
            existing.RoomUid = roomUid;
            existing.Success = success;
            existing.Delete = delete;
            return (true, existing);
        });
}

/// <summary>Managed counterpart of native DECL_DATA(KWeddingItemInfo).</summary>
public sealed class KWeddingItemInfo
{
    public long ItemUid { get; set; }
    public int WeddingUid { get; set; }
    public sbyte WeddingHallType { get; set; }
    public sbyte OfficiantNpc { get; set; }
    public long Groom { get; set; }
    public long Bride { get; set; }
    public string GroomName { get; set; } = string.Empty;
    public string BrideName { get; set; } = string.Empty;
    public string WeddingDate { get; set; } = string.Empty;
    public string WeddingMessage { get; set; } = string.Empty;

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.ItemUid);
            ser.Put(value.WeddingUid);
            ser.Put(value.WeddingHallType);
            ser.Put(value.OfficiantNpc);
            ser.Put(value.Groom);
            ser.Put(value.Bride);
            ser.PutWString(value.GroomName);
            ser.PutWString(value.BrideName);
            ser.PutWString(value.WeddingDate);
            ser.PutWString(value.WeddingMessage);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KWeddingItemInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out long itemUid) ||
                !ser.TryGet(out int weddingUid) ||
                !ser.TryGet(out sbyte hallType) ||
                !ser.TryGet(out sbyte officiantNpc) ||
                !ser.TryGet(out long groom) ||
                !ser.TryGet(out long bride) ||
                !ser.TryGetWString(out var groomName) ||
                !ser.TryGetWString(out var brideName) ||
                !ser.TryGetWString(out var weddingDate) ||
                !ser.TryGetWString(out var weddingMessage))
                return (false, existing);
            existing.ItemUid = itemUid;
            existing.WeddingUid = weddingUid;
            existing.WeddingHallType = hallType;
            existing.OfficiantNpc = officiantNpc;
            existing.Groom = groom;
            existing.Bride = bride;
            existing.GroomName = groomName;
            existing.BrideName = brideName;
            existing.WeddingDate = weddingDate;
            existing.WeddingMessage = weddingMessage;
            return (true, existing);
        });
}

/// <summary>Managed counterpart of native DECL_DATA(KRelationshipInfo).</summary>
public sealed class KRelationshipInfo
{
    public sbyte RelationshipType { get; set; }
    public long OtherUnitUid { get; set; }
    public string OtherNickName { get; set; } = string.Empty;
    public string LoveWord { get; set; } = string.Empty;
    public long Date { get; set; }
    public long LastReward { get; set; }
    public sbyte RewardTitleType { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.RelationshipType);
            ser.Put(value.OtherUnitUid);
            ser.PutWString(value.OtherNickName);
            ser.PutWString(value.LoveWord);
            ser.Put(value.Date);
            ser.Put(value.LastReward);
            ser.Put(value.RewardTitleType);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KRelationshipInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out sbyte relationshipType) ||
                !ser.TryGet(out long otherUnitUid) ||
                !ser.TryGetWString(out var nickname) ||
                !ser.TryGetWString(out var loveWord) ||
                !ser.TryGet(out long date) ||
                !ser.TryGet(out long lastReward) ||
                !ser.TryGet(out sbyte rewardTitleType))
                return (false, existing);
            existing.RelationshipType = relationshipType;
            existing.OtherUnitUid = otherUnitUid;
            existing.OtherNickName = nickname;
            existing.LoveWord = loveWord;
            existing.Date = date;
            existing.LastReward = lastReward;
            existing.RewardTitleType = rewardTitleType;
            return (true, existing);
        });
}

/// <summary>Managed counterpart of native DECL_DATA(KDBConnectionInfo).</summary>
public sealed class KDbConnectionInfo
{
    public int DbType { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public int ThreadCount { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.DbType);
            ser.PutWString(value.ConnectionString);
            ser.Put(value.ThreadCount);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KDbConnectionInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int dbType) ||
                !ser.TryGetWString(out var connectionString) ||
                !ser.TryGet(out int threadCount))
                return (false, existing);
            existing.DbType = dbType;
            existing.ConnectionString = connectionString;
            existing.ThreadCount = threadCount;
            return (true, existing);
        });
    }
}
