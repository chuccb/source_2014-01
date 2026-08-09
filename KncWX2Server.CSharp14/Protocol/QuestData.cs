namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Quest-reform value stored by KSubQuestInfo.</summary>
public sealed class KSubQuestData
{
    public int InInventoryItemCount { get; set; }
    public bool Success { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.InInventoryItemCount);
            ser.Put(value.Success);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KSubQuestData value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            if (!ser.TryGet(out int count) || !ser.TryGet(out bool success))
                return (false, x);

            x.InInventoryItemCount = count;
            x.Success = success;
            return (true, x);
        });
    }
}

/// <summary>
/// Native KSubQuestInfo. SERV_REFORM_QUEST changes only the map value type:
/// map&lt;int,KSubQuestData&gt; versus map&lt;int,int&gt;.
/// </summary>
public sealed class KSubQuestInfo
{
    public SortedDictionary<int, KSubQuestData> ReformSubQuestInfo { get; } = new();
    public SortedDictionary<int, int> LegacySubQuestInfo { get; } = new();

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            // The callback cannot capture options because the native USERCLASS
            // wrapper must be emitted before the payload. Use the instance's
            // configured map representation through the overload below.
            return value.SerializePayload(ser, options);
        });

    private bool SerializePayload(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        var stl = new NativeStlSerializer(serializer);

        if (options.ReformQuest)
        {
            stl.PutMap(
                ReformSubQuestInfo,
                static (ser, key) => ser.Put(key),
                static (ser, value) => value.Serialize(ser));
        }
        else
        {
            stl.PutMap(
                LegacySubQuestInfo,
                static (ser, key) => ser.Put(key),
                static (ser, value) => ser.Put(value));
        }

        return true;
    }

    public static bool TryDeserialize(
        NativePrimitiveSerializer serializer,
        ProtocolOptions options,
        out KSubQuestInfo value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, x) =>
        {
            return x.DeserializePayload(ser, options)
                ? (true, x)
                : (false, x);
        });
    }

    private bool DeserializePayload(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        var stl = new NativeStlSerializer(serializer);

        if (options.ReformQuest)
        {
            if (!stl.TryGetMap(
                    out SortedDictionary<int, KSubQuestData> map,
                    static ser => ser.TryGet(out int key) ? (true, key) : (false, default),
                    static ser => KSubQuestData.TryDeserialize(ser, out var item)
                        ? (true, item)
                        : (false, default!)))
                return false;

            foreach (var pair in map)
                ReformSubQuestInfo.TryAdd(pair.Key, pair.Value);
        }
        else
        {
            if (!stl.TryGetMap(
                    out SortedDictionary<int, int> map,
                    static ser => ser.TryGet(out int key) ? (true, key) : (false, default),
                    static ser => ser.TryGet(out int item) ? (true, item) : (false, default)))
                return false;

            foreach (var pair in map)
                LegacySubQuestInfo.TryAdd(pair.Key, pair.Value);
        }

        return true;
    }
}
