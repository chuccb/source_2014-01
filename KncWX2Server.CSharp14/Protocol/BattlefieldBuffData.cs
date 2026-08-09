namespace KncWX2Server.CSharp14.Protocol;

public sealed class KLastPositionInfo
{
    public int MapId { get; set; }
    public byte LastTouchLineIndex { get; set; }
    public ushort LastPosValue { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.MapId); ser.Put(value.LastTouchLineIndex); ser.Put(value.LastPosValue); return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KLastPositionInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int map) || !ser.TryGet(out byte line) || !ser.TryGet(out ushort pos)) return (false, x);
            x.MapId = map; x.LastTouchLineIndex = line; x.LastPosValue = pos; return (true, x);
        });
    }
}

public sealed class KBuffBehaviorFactor
{
    public uint Type { get; set; }
    public List<float> Values { get; set; } = [];

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
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out uint type)) return (false, x);
            var stl = new NativeStlSerializer(ser);
            if (!stl.TryGetVector(out List<float> values, static s => s.TryGet(out float item) ? (true, item) : (false, default))) return (false, x);
            x.Type = type; x.Values = values; return (true, x);
        });
    }
}

public sealed class KBuffFinalizerFactor
{
    public uint Type { get; set; }
    public List<float> Values { get; set; } = [];

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
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out uint type)) return (false, x);
            var stl = new NativeStlSerializer(ser);
            if (!stl.TryGetVector(out List<float> values, static s => s.TryGet(out float item) ? (true, item) : (false, default))) return (false, x);
            x.Type = type; x.Values = values; return (true, x);
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
            ser.Put(value.BuffTempletId); ser.Put(value.UniqueNum); return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KBuffIdentity value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int id) || !ser.TryGet(out uint unique)) return (false, x);
            x.BuffTempletId = id; x.UniqueNum = unique; return (true, x);
        });
    }
}
