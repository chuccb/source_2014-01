namespace KncWX2Server.CSharp14.Protocol;

public sealed class KBuffBehaviorFactor
{
    public uint Type { get; set; }
    public List<float> Values { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.Type);
            new NativeStlSerializer(ser).PutVector(value.Values, static (s, item) => s.Put(item));
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KBuffBehaviorFactor value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out uint type) ||
                !new NativeStlSerializer(ser).TryGetVector(out List<float> values,
                    static s => s.TryGet(out float item) ? (true, item) : (false, 0f)))
                return (false, existing);
            existing.Type = type;
            existing.Values.Clear();
            existing.Values.AddRange(values);
            return (true, existing);
        });
    }
}

public sealed class KBuffFinalizerFactor
{
    public uint Type { get; set; }
    public List<float> Values { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.Type);
            new NativeStlSerializer(ser).PutVector(value.Values, static (s, item) => s.Put(item));
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KBuffFinalizerFactor value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out uint type) ||
                !new NativeStlSerializer(ser).TryGetVector(out List<float> values,
                    static s => s.TryGet(out float item) ? (true, item) : (false, 0f)))
                return (false, existing);
            existing.Type = type;
            existing.Values.Clear();
            existing.Values.AddRange(values);
            return (true, existing);
        });
    }
}

public sealed class KBuffIdentity
{
    public int BuffTempletId { get; set; }
    public uint UniqueNum { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.BuffTempletId);
            ser.Put(value.UniqueNum);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KBuffIdentity value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int id) || !ser.TryGet(out uint uniqueNum))
                return (false, existing);
            existing.BuffTempletId = id;
            existing.UniqueNum = uniqueNum;
            return (true, existing);
        });
    }
}

public sealed class KBuffFactor
{
    public List<KBuffBehaviorFactor> BuffBehaviorFactors { get; } = [];
    public List<KBuffFinalizerFactor> BuffFinalizerFactors { get; } = [];
    public KBuffIdentity BuffIdentity { get; set; } = new();
    public long MesGameUnitUid { get; set; }
    public float AccumulationMultiplier { get; set; }
    public byte AccumulationCountNow { get; set; }
    public bool IsMesGameUnitNpc { get; set; }
    public int FactorId { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            var stl = new NativeStlSerializer(ser);
            stl.PutVector(value.BuffBehaviorFactors, static (s, item) => item.Serialize(s));
            stl.PutVector(value.BuffFinalizerFactors, static (s, item) => item.Serialize(s));
            value.BuffIdentity.Serialize(ser);
            ser.Put(value.MesGameUnitUid);
            ser.Put(value.AccumulationMultiplier);
            ser.Put(value.AccumulationCountNow);
            ser.Put(value.IsMesGameUnitNpc);
            ser.Put(value.FactorId);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KBuffFactor value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            var stl = new NativeStlSerializer(ser);
            if (!stl.TryGetVector(out List<KBuffBehaviorFactor> behaviors,
                    static s => KBuffBehaviorFactor.TryDeserialize(s, out var item) ? (true, item) : (false, new KBuffBehaviorFactor())) ||
                !stl.TryGetVector(out List<KBuffFinalizerFactor> finalizers,
                    static s => KBuffFinalizerFactor.TryDeserialize(s, out var item) ? (true, item) : (false, new KBuffFinalizerFactor())) ||
                !KBuffIdentity.TryDeserialize(ser, out var identity) ||
                !ser.TryGet(out long mesGameUnitUid) ||
                !ser.TryGet(out float accumulationMultiplier) ||
                !ser.TryGet(out byte accumulationCountNow) ||
                !ser.TryGet(out bool isMesGameUnitNpc) ||
                !ser.TryGet(out int factorId))
                return (false, existing);

            existing.BuffBehaviorFactors.Clear();
            existing.BuffBehaviorFactors.AddRange(behaviors);
            existing.BuffFinalizerFactors.Clear();
            existing.BuffFinalizerFactors.AddRange(finalizers);
            existing.BuffIdentity = identity;
            existing.MesGameUnitUid = mesGameUnitUid;
            existing.AccumulationMultiplier = accumulationMultiplier;
            existing.AccumulationCountNow = accumulationCountNow;
            existing.IsMesGameUnitNpc = isMesGameUnitNpc;
            existing.FactorId = factorId;
            return (true, existing);
        });
    }
}

public sealed class KBuffInfo
{
    public KBuffFactor FactorInfo { get; set; } = new();
    public long BuffStartTime { get; set; }
    public long BuffEndTime { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            value.FactorInfo.Serialize(ser);
            ser.Put(value.BuffStartTime);
            ser.Put(value.BuffEndTime);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KBuffInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!KBuffFactor.TryDeserialize(ser, out var factor) ||
                !ser.TryGet(out long start) || !ser.TryGet(out long end))
                return (false, existing);
            existing.FactorInfo = factor;
            existing.BuffStartTime = start;
            existing.BuffEndTime = end;
            return (true, existing);
        });
    }
}

public sealed class KUnitBuffInfo
{
    public long InsertTime { get; set; }
    public SortedDictionary<int, KBuffInfo> BuffInfo { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.InsertTime);
            new NativeStlSerializer(ser).PutMap(value.BuffInfo,
                static (s, key) => s.Put(key),
                static (s, item) => item.Serialize(s));
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KUnitBuffInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out long insertTime) ||
                !new NativeStlSerializer(ser).TryGetMap(out SortedDictionary<int, KBuffInfo> buffInfo,
                    static s => s.TryGet(out int key) ? (true, key) : (false, 0),
                    static s => KBuffInfo.TryDeserialize(s, out var item) ? (true, item) : (false, new KBuffInfo())))
                return (false, existing);
            existing.InsertTime = insertTime;
            existing.BuffInfo.Clear();
            foreach (var pair in buffInfo)
                existing.BuffInfo.TryAdd(pair.Key, pair.Value);
            return (true, existing);
        });
    }
}

public sealed class KNpcUnitBuffInfo
{
    public int NpcUid { get; set; }
    public List<KBuffFactor> BuffFactors { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.NpcUid);
            new NativeStlSerializer(ser).PutVector(value.BuffFactors, static (s, item) => item.Serialize(s));
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KNpcUnitBuffInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int npcUid) ||
                !new NativeStlSerializer(ser).TryGetVector(out List<KBuffFactor> factors,
                    static s => KBuffFactor.TryDeserialize(s, out var item) ? (true, item) : (false, new KBuffFactor())))
                return (false, existing);
            existing.NpcUid = npcUid;
            existing.BuffFactors.Clear();
            existing.BuffFactors.AddRange(factors);
            return (true, existing);
        });
    }
}
