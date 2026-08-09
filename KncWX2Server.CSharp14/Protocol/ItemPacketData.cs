namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Managed counterpart of native DECL_DATA(KItemInfo).</summary>
public sealed class KItemInfo
{
    public int ItemId { get; set; }
    public sbyte UsageType { get; set; }
    public int Quantity { get; set; } = 1;
    public short Endurance { get; set; }
    public byte SealData { get; set; }
    public sbyte EnchantLevel { get; set; }
    public KItemAttributeEnchantInfo AttributeEnchantInfo { get; set; } = new();
    public List<short> ItemSocket { get; set; } = [];
    public List<int> RandomSocket { get; set; } = [];
    public sbyte ItemState { get; set; }
    public short Period { get; set; }
    public string ExpirationDate { get; set; } = string.Empty;
    public long GoldTicketKeyUid { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) => Serialize(serializer, ProtocolOptions.Default);

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) => value.SerializeFields(ser, options));
    }

    public bool SerializeWithOptions(NativePrimitiveSerializer serializer, ProtocolOptions options) =>
        Serialize(serializer, options);

    private bool SerializeFields(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        serializer.Put(ItemId);
        serializer.Put(UsageType);
        serializer.Put(Quantity);
        serializer.Put(Endurance);
        serializer.Put(SealData);
        serializer.Put(EnchantLevel);
        if (!AttributeEnchantInfo.Serialize(serializer)) return false;

        var stl = new NativeStlSerializer(serializer);
        if (options.ItemOptionDataSize)
            stl.PutVector(ItemSocket.Select(static x => (int)x).ToArray(), static (ser, x) => ser.Put(x));
        else
            stl.PutVector(ItemSocket, static (ser, x) => ser.Put(x));

        if (options.NewItemSystem201305)
        {
            stl.PutVector(RandomSocket, static (ser, x) => ser.Put(x));
            serializer.Put(ItemState);
        }

        serializer.Put(Period);
        serializer.PutWString(ExpirationDate);
        if (options.GoldTicket)
            serializer.Put(GoldTicketKeyUid);
        return true;
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KItemInfo value) =>
        TryDeserialize(serializer, ProtocolOptions.Default, out value);

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KItemInfo value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value,
            ser => TryDeserializeFields(ser, options, value));
    }

    private static (bool Ok, KItemInfo Value) TryDeserializeFields(
        NativePrimitiveSerializer serializer, ProtocolOptions options, KItemInfo value)
    {
        if (!serializer.TryGet(out int itemId) || !serializer.TryGet(out sbyte usageType) ||
            !serializer.TryGet(out int quantity) || !serializer.TryGet(out short endurance) ||
            !serializer.TryGet(out byte sealData) || !serializer.TryGet(out sbyte enchantLevel) ||
            !KItemAttributeEnchantInfo.TryDeserialize(serializer, out var attributeEnchantInfo))
            return (false, value);

        var stl = new NativeStlSerializer(serializer);
        List<short> itemSocket;
        if (options.ItemOptionDataSize)
        {
            if (!stl.TryGetVector(out List<int> socketInts, static ser =>
                    ser.TryGet(out int item) ? (true, item) : (false, default)))
                return (false, value);
            itemSocket = socketInts.Select(static x => checked((short)x)).ToList();
        }
        else if (!stl.TryGetVector(out itemSocket, static ser =>
                     ser.TryGet(out short item) ? (true, item) : (false, default)))
        {
            return (false, value);
        }

        List<int> randomSocket = [];
        sbyte itemState = 0;
        if (options.NewItemSystem201305)
        {
            if (!stl.TryGetVector(out randomSocket, static ser =>
                    ser.TryGet(out int item) ? (true, item) : (false, default)) ||
                !serializer.TryGet(out itemState))
                return (false, value);
        }

        if (!serializer.TryGet(out short period) || !serializer.TryGetWString(out var expirationDate))
            return (false, value);

        long goldTicketKeyUid = 0;
        if (options.GoldTicket && !serializer.TryGet(out goldTicketKeyUid))
            return (false, value);

        value.ItemId = itemId;
        value.UsageType = usageType;
        value.Quantity = quantity;
        value.Endurance = endurance;
        value.SealData = sealData;
        value.EnchantLevel = enchantLevel;
        value.AttributeEnchantInfo = attributeEnchantInfo;
        value.ItemSocket = itemSocket;
        value.RandomSocket = randomSocket;
        value.ItemState = itemState;
        value.Period = period;
        value.ExpirationDate = expirationDate;
        value.GoldTicketKeyUid = goldTicketKeyUid;
        return (true, value);
    }
}

public sealed class KInventoryItemSimpleInfo
{
    public long ItemUid { get; set; }
    public int ItemId { get; set; }
    public sbyte SlotCategory { get; set; }
    public sbyte SlotId { get; set; }
    public short ExpandedSlotId { get; set; }
    public sbyte EnchantLevel { get; set; }
    public KItemAttributeEnchantInfo AttributeEnchantInfo { get; set; } = new();

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.ItemUid);
            ser.Put(value.ItemId);
            ser.Put(value.SlotCategory);
            if (options.ExpandSlotIdDataSize) ser.Put(value.ExpandedSlotId); else ser.Put(value.SlotId);
            ser.Put(value.EnchantLevel);
            return value.AttributeEnchantInfo.Serialize(ser);
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KInventoryItemSimpleInfo value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, ser =>
        {
            if (!ser.TryGet(out long uid) || !ser.TryGet(out int itemId) || !ser.TryGet(out sbyte slotCategory))
                return (false, value);
            if (options.ExpandSlotIdDataSize)
            {
                if (!ser.TryGet(out short slotId)) return (false, value);
                value.ExpandedSlotId = slotId;
            }
            else
            {
                if (!ser.TryGet(out sbyte slotId)) return (false, value);
                value.SlotId = slotId;
            }
            if (!ser.TryGet(out sbyte enchantLevel) || !KItemAttributeEnchantInfo.TryDeserialize(ser, out var attr))
                return (false, value);
            value.ItemUid = uid;
            value.ItemId = itemId;
            value.SlotCategory = slotCategory;
            value.EnchantLevel = enchantLevel;
            value.AttributeEnchantInfo = attr;
            return (true, value);
        });
    }
}

public sealed class KInventoryItemInfo
{
    public long ItemUid { get; set; }
    public sbyte SlotCategory { get; set; }
    public sbyte SlotId { get; set; }
    public short ExpandedSlotId { get; set; }
    public KItemInfo ItemInfo { get; set; } = new();

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.ItemUid);
            ser.Put(value.SlotCategory);
            if (options.ExpandSlotIdDataSize) ser.Put(value.ExpandedSlotId); else ser.Put(value.SlotId);
            return value.ItemInfo.Serialize(ser, options);
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KInventoryItemInfo value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, ser =>
        {
            if (!ser.TryGet(out long uid) || !ser.TryGet(out sbyte slotCategory))
                return (false, value);
            if (options.ExpandSlotIdDataSize)
            {
                if (!ser.TryGet(out short slotId)) return (false, value);
                value.ExpandedSlotId = slotId;
            }
            else
            {
                if (!ser.TryGet(out sbyte slotId)) return (false, value);
                value.SlotId = slotId;
            }
            if (!KItemInfo.TryDeserialize(ser, options, out var itemInfo)) return (false, value);
            value.ItemUid = uid;
            value.SlotCategory = slotCategory;
            value.ItemInfo = itemInfo;
            return (true, value);
        });
    }
}
