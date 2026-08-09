namespace KncWX2Server.CSharp14.Protocol;

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
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out long itemUid) || !ser.TryGet(out byte deleteReason))
                return (false, existing);
            existing.ItemUid = itemUid;
            existing.DeleteReason = deleteReason;
            return (true, existing);
        });
    }
}

public sealed class KItemQuantityUpdate
{
    public SortedDictionary<long, int> QuantityChange { get; set; } = [];
    public List<long> DeletedItemUids { get; set; } = [];
    public List<KDeletedItemInfo> DeletedItems { get; set; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            var stl = new NativeStlSerializer(ser);
            stl.PutMap(value.QuantityChange,
                static (s, key) => s.Put(key),
                static (s, item) => s.Put(item));

            if (options.DeleteItem)
                stl.PutVector(value.DeletedItems, static (s, item) => item.Serialize(s));
            else
                stl.PutVector(value.DeletedItemUids, static (s, item) => s.Put(item));

            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KItemQuantityUpdate value,
        ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            var stl = new NativeStlSerializer(ser);
            if (!stl.TryGetMap(out SortedDictionary<long, int> quantityChange,
                static s => s.TryGet(out long key) ? (true, key) : (false, default),
                static s => s.TryGet(out int item) ? (true, item) : (false, default)))
                return (false, existing);

            existing.QuantityChange = quantityChange;

            if (options.DeleteItem)
            {
                if (!stl.TryGetVector(out List<KDeletedItemInfo> deletedItems,
                    static s => KDeletedItemInfo.TryDeserialize(s, out var item)
                        ? (true, item)
                        : (false, default!)))
                    return (false, existing);
                existing.DeletedItems = deletedItems;
            }
            else
            {
                if (!stl.TryGetVector(out List<long> deletedItemUids,
                    static s => s.TryGet(out long item) ? (true, item) : (false, default)))
                    return (false, existing);
                existing.DeletedItemUids = deletedItemUids;
            }

            return (true, existing);
        });
    }
}
