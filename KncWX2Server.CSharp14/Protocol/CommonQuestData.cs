namespace KncWX2Server.CSharp14.Protocol;

public sealed class KCompleteQuestInfo
{
    public int QuestId { get; set; }
    public int CompleteCount { get; set; }
    public long CompleteDate { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.QuestId);
            ser.Put(value.CompleteCount);
            ser.Put(value.CompleteDate);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KCompleteQuestInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int questId) || !ser.TryGet(out int completeCount) || !ser.TryGet(out long completeDate))
                return (false, existing);
            existing.QuestId = questId;
            existing.CompleteCount = completeCount;
            existing.CompleteDate = completeDate;
            return (true, existing);
        });
    }
}

public sealed class KSubQuestInstance
{
    public int Id { get; set; }
    public byte ClearData { get; set; }
    public bool IsSuccess { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.Id);
            ser.Put(value.ClearData);
            ser.Put(value.IsSuccess);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KSubQuestInstance value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int id) || !ser.TryGet(out byte clearData) || !ser.TryGet(out bool success))
                return (false, existing);
            existing.Id = id;
            existing.ClearData = clearData;
            existing.IsSuccess = success;
            return (true, existing);
        });
    }
}

public sealed class KQuestInstance
{
    public int Id { get; set; }
    public long OwnerUnitUid { get; set; }
    public List<KSubQuestInstance> SubQuestInstances { get; set; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.Id);
            ser.Put(value.OwnerUnitUid);
            new NativeStlSerializer(ser).PutVector(value.SubQuestInstances,
                static (s, item) => item.Serialize(s));
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KQuestInstance value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int id) || !ser.TryGet(out long ownerUid))
                return (false, existing);

            var stl = new NativeStlSerializer(ser);
            if (!stl.TryGetVector(out List<KSubQuestInstance> subQuestInstances,
                static s => KSubQuestInstance.TryDeserialize(s, out var item)
                    ? (true, item)
                    : (false, default!)))
                return (false, existing);

            existing.Id = id;
            existing.OwnerUnitUid = ownerUid;
            existing.SubQuestInstances = subQuestInstances;
            return (true, existing);
        });
    }
}

public sealed class KDungeonClearInfo
{
    public int DungeonId { get; set; }
    public int MaxScore { get; set; }
    public sbyte MaxTotalRank { get; set; }
    public string ClearTime { get; set; } = string.Empty;
    public bool IsNew { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.DungeonId);
            ser.Put(value.MaxScore);
            ser.Put(value.MaxTotalRank);
            ser.PutWString(value.ClearTime);
            ser.Put(value.IsNew);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KDungeonClearInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int dungeonId) || !ser.TryGet(out int maxScore) ||
                !ser.TryGet(out sbyte maxTotalRank) || !ser.TryGetWString(out var clearTime) ||
                !ser.TryGet(out bool isNew))
                return (false, existing);
            existing.DungeonId = dungeonId;
            existing.MaxScore = maxScore;
            existing.MaxTotalRank = maxTotalRank;
            existing.ClearTime = clearTime;
            existing.IsNew = isNew;
            return (true, existing);
        });
    }
}

public sealed class KDungeonPlayInfo
{
    public int DungeonId { get; set; }
    public int PlayTimes { get; set; }
    public int ClearTimes { get; set; }
    public bool IsNew { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.DungeonId);
            ser.Put(value.PlayTimes);
            ser.Put(value.ClearTimes);
            ser.Put(value.IsNew);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KDungeonPlayInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int dungeonId) || !ser.TryGet(out int playTimes) ||
                !ser.TryGet(out int clearTimes) || !ser.TryGet(out bool isNew))
                return (false, existing);
            existing.DungeonId = dungeonId;
            existing.PlayTimes = playTimes;
            existing.ClearTimes = clearTimes;
            existing.IsNew = isNew;
            return (true, existing);
        });
    }
}

public sealed class KTCClearInfo
{
    public int TcId { get; set; }
    public string ClearTime { get; set; } = string.Empty;
    public bool IsNew { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.TcId);
            ser.PutWString(value.ClearTime);
            ser.Put(value.IsNew);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KTCClearInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int tcId) || !ser.TryGetWString(out var clearTime) || !ser.TryGet(out bool isNew))
                return (false, existing);
            existing.TcId = tcId;
            existing.ClearTime = clearTime;
            existing.IsNew = isNew;
            return (true, existing);
        });
    }
}
