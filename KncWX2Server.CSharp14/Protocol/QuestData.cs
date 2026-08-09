namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Quest-reform value stored by <see cref="KSubQuestInfo"/>.</summary>
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

    public static bool TryDeserialize(
        NativePrimitiveSerializer serializer,
        out KSubQuestData value)
    {
        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!TryReadFields(ser, out var fields))
                return (false, existing);

            existing.InInventoryItemCount = fields.InInventoryItemCount;
            existing.Success = fields.Success;
            return (true, existing);
        });
    }

    private static bool TryReadFields(
        NativePrimitiveSerializer serializer,
        out Fields fields)
    {
        if (!serializer.TryGet(out int inInventoryItemCount) ||
            !serializer.TryGet(out bool success))
        {
            fields = default;
            return false;
        }

        fields = new(inInventoryItemCount, success);
        return true;
    }

    private readonly record struct Fields(
        int InInventoryItemCount,
        bool Success);
}

/// <summary>
/// Native KSubQuestInfo. SERV_REFORM_QUEST changes only the map value type:
/// map&lt;int,KSubQuestData&gt; versus map&lt;int,int&gt;.
/// </summary>
public sealed class KSubQuestInfo
{
    public SortedDictionary<int, KSubQuestData> ReformSubQuestInfo { get; } = [];
    public SortedDictionary<int, int> LegacySubQuestInfo { get; } = [];

    public bool Serialize(
        NativePrimitiveSerializer serializer,
        ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new NativeUserClassSerializer(serializer).Put(
            this,
            (ser, value) => value.SerializePayload(ser, options));
    }

    private bool SerializePayload(
        NativePrimitiveSerializer serializer,
        ProtocolOptions options)
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

        return new NativeUserClassSerializer(serializer).TryGet(
            out value,
            (ser, existing) => existing.TryDeserializePayload(ser, options)
                ? (true, existing)
                : (false, existing));
    }

    private bool TryDeserializePayload(
        NativePrimitiveSerializer serializer,
        ProtocolOptions options)
    {
        var stl = new NativeStlSerializer(serializer);

        if (options.ReformQuest)
        {
            if (!stl.TryGetMap(
                    out SortedDictionary<int, KSubQuestData> map,
                    GetInt32,
                    static ser => KSubQuestData.TryDeserialize(ser, out var item)
                        ? (true, item)
                        : (false, default!)))
            {
                return false;
            }

            ReformSubQuestInfo.Clear();
            foreach (var pair in map)
                ReformSubQuestInfo.Add(pair.Key, pair.Value);
        }
        else
        {
            if (!stl.TryGetMap(
                    out SortedDictionary<int, int> map,
                    GetInt32,
                    GetInt32))
            {
                return false;
            }

            LegacySubQuestInfo.Clear();
            foreach (var pair in map)
                LegacySubQuestInfo.Add(pair.Key, pair.Value);
        }

        return true;
    }

    private static (bool Ok, int Value) GetInt32(NativePrimitiveSerializer serializer) =>
        serializer.TryGet(out int value)
            ? (true, value)
            : (false, default);
}
