namespace KncWX2Server.CSharp14.Protocol;

public sealed class KDBConnectionInfo
{
    public int DbType { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public int ThreadCount { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer)
    {
        return new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.DbType);
            ser.PutWString(value.ConnectionString);
            ser.Put(value.ThreadCount);
            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KDBConnectionInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int dbType) || !ser.TryGetWString(out var connectionString) || !ser.TryGet(out int threadCount))
                return (false, existing);
            existing.DbType = dbType;
            existing.ConnectionString = connectionString;
            existing.ThreadCount = threadCount;
            return (true, existing);
        });
    }
}

public sealed class KWeddingHallInfo
{
    public int WeddingUid { get; set; }
    public sbyte WeddingHallType { get; set; }
    public sbyte OfficiantNpc { get; set; }
    public long Groom { get; set; }
    public long Bride { get; set; }
    public string WeddingDate { get; set; } = string.Empty;
    public long WeddingDateTimestamp { get; set; }
    public string WeddingMessage { get; set; } = string.Empty;
    public long RoomUid { get; set; }
    public bool Success { get; set; }
    public bool Delete { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        if (!options.RelationshipSystem) return false;
        return new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.WeddingUid);
            ser.Put(value.WeddingHallType);
            ser.Put(value.OfficiantNpc);
            ser.Put(value.Groom);
            ser.Put(value.Bride);
            ser.PutWString(value.WeddingDate);
            ser.Put(value.WeddingDateTimestamp);
            ser.PutWString(value.WeddingMessage);
            ser.Put(value.RoomUid);
            ser.Put(value.Success);
            ser.Put(value.Delete);
            return true;
        });
    }
}

public sealed class KRelationshipInfo
{
    public sbyte RelationshipType { get; set; }
    public long OtherUnitUid { get; set; }
    public string OtherNickName { get; set; } = string.Empty;
    public string LoveWord { get; set; } = string.Empty;
    public long Date { get; set; }
    public long LastReward { get; set; }
    public sbyte RewardTitleType { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        if (!options.RelationshipSystem) return false;
        return new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
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
    }
}
