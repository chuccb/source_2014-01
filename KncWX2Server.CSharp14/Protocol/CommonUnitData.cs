namespace KncWX2Server.CSharp14.Protocol;

public sealed class KStat
{
    public int BaseHp { get; set; }
    public int AtkPhysic { get; set; }
    public int AtkMagic { get; set; }
    public int DefPhysic { get; set; }
    public int DefMagic { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.BaseHp);
            ser.Put(value.AtkPhysic);
            ser.Put(value.AtkMagic);
            ser.Put(value.DefPhysic);
            ser.Put(value.DefMagic);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KStat value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int baseHp) || !ser.TryGet(out int atkPhysic) ||
                !ser.TryGet(out int atkMagic) || !ser.TryGet(out int defPhysic) ||
                !ser.TryGet(out int defMagic)) return (false, x);
            x.BaseHp = baseHp;
            x.AtkPhysic = atkPhysic;
            x.AtkMagic = atkMagic;
            x.DefPhysic = defPhysic;
            x.DefMagic = defMagic;
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
            ser.Put(value.GuildUid);
            ser.PutWString(value.GuildName);
            ser.Put(value.MembershipGrade);
            ser.Put(value.HonorPoint);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KUserGuildInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int guildUid) || !ser.TryGetWString(out var guildName) ||
                !ser.TryGet(out byte membershipGrade) || !ser.TryGet(out int honorPoint))
                return (false, x);
            x.GuildUid = guildUid;
            x.GuildName = guildName;
            x.MembershipGrade = membershipGrade;
            x.HonorPoint = honorPoint;
            return (true, x);
        });
    }
}

public sealed class KDeletedItemInfo
{
    public long ItemUid { get; set; }
    public byte DeleteReason { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.ItemUid);
            ser.Put(value.DeleteReason);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KDeletedItemInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out long itemUid) || !ser.TryGet(out byte deleteReason)) return (false, x);
            x.ItemUid = itemUid;
            x.DeleteReason = deleteReason;
            return (true, x);
        });
    }
}

public sealed class KItemQuantityUpdate
{
    public SortedDictionary<long, int> QuantityChange { get; } = [];
    public List<KDeletedItemInfo> Deleted { get; } = [];
    public List<long> DeletedLegacy { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            var stl = new NativeStlSerializer(ser);
            stl.PutMap(value.QuantityChange, static (s, key) => s.Put(key), static (s, item) => s.Put(item));
            if (options.DeleteItem)
                stl.PutVector(value.Deleted, static (s, item) => item.Serialize(s));
            else
                stl.PutVector(value.DeletedLegacy, static (s, itemUid) => s.Put(itemUid));
            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KItemQuantityUpdate value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, x) =>
        {
            var stl = new NativeStlSerializer(ser);
            if (!stl.TryGetMap(out SortedDictionary<long, int> quantityChange,
                    static s => s.TryGet(out long v) ? (true, v) : (false, 0),
                    static s => s.TryGet(out int v) ? (true, v) : (false, 0))) return (false, x);
            x.QuantityChange.Clear();
            foreach (var pair in quantityChange) x.QuantityChange.TryAdd(pair.Key, pair.Value);
            if (options.DeleteItem)
            {
                if (!stl.TryGetVector(out List<KDeletedItemInfo> deleted,
                        static s => KDeletedItemInfo.TryDeserialize(s, out var item) ? (true, item) : (false, new KDeletedItemInfo()))) return (false, x);
                x.Deleted.Clear();
                x.Deleted.AddRange(deleted);
                x.DeletedLegacy.Clear();
            }
            else
            {
                if (!stl.TryGetVector(out List<long> deletedLegacy,
                        static s => s.TryGet(out long itemUid) ? (true, itemUid) : (false, 0))) return (false, x);
                x.DeletedLegacy.Clear();
                x.DeletedLegacy.AddRange(deletedLegacy);
                x.Deleted.Clear();
            }
            return (true, x);
        });
    }
}

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
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int questId) || !ser.TryGet(out int completeCount) || !ser.TryGet(out long completeDate)) return (false, x);
            x.QuestId = questId;
            x.CompleteCount = completeCount;
            x.CompleteDate = completeDate;
            return (true, x);
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
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int id) || !ser.TryGet(out byte clearData) || !ser.TryGet(out bool isSuccess)) return (false, x);
            x.Id = id;
            x.ClearData = clearData;
            x.IsSuccess = isSuccess;
            return (true, x);
        });
    }
}

public sealed class KQuestInstance
{
    public int Id { get; set; }
    public long OwnerUnitUid { get; set; }
    public List<KSubQuestInstance> SubQuestInstances { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.Id);
            ser.Put(value.OwnerUnitUid);
            new NativeStlSerializer(ser).PutVector(value.SubQuestInstances, static (s, item) => item.Serialize(s));
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KQuestInstance value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int id) || !ser.TryGet(out long ownerUnitUid) ||
                !new NativeStlSerializer(ser).TryGetVector(out List<KSubQuestInstance> subQuestInstances,
                    static s => KSubQuestInstance.TryDeserialize(s, out var item) ? (true, item) : (false, new KSubQuestInstance()))) return (false, x);
            x.Id = id;
            x.OwnerUnitUid = ownerUnitUid;
            x.SubQuestInstances.Clear();
            x.SubQuestInstances.AddRange(subQuestInstances);
            return (true, x);
        });
    }
}
