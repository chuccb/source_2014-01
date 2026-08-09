namespace KncWX2Server.CSharp14.Protocol;

public sealed class KBuyGPItemInfo
{
    public int ItemId { get; set; }
    public int Price { get; set; }
    public int PvpPoint { get; set; }
    public sbyte PeriodType { get; set; }
    public int Quantity { get; set; }
    public short Endurance { get; set; }
    public short Period { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.ItemId); ser.Put(value.Price); ser.Put(value.PvpPoint); ser.Put(value.PeriodType);
            ser.Put(value.Quantity); ser.Put(value.Endurance); ser.Put(value.Period); return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KBuyGPItemInfo value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int itemId) || !ser.TryGet(out int price) || !ser.TryGet(out int pvpPoint) ||
                !ser.TryGet(out sbyte periodType) || !ser.TryGet(out int quantity) ||
                !ser.TryGet(out short endurance) || !ser.TryGet(out short period)) return (false, existing);
            existing.ItemId = itemId; existing.Price = price; existing.PvpPoint = pvpPoint; existing.PeriodType = periodType;
            existing.Quantity = quantity; existing.Endurance = endurance; existing.Period = period;
            return (true, existing);
        });
    }
}

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
            ser.Put(value.BaseHp); ser.Put(value.AtkPhysic); ser.Put(value.AtkMagic); ser.Put(value.DefPhysic); ser.Put(value.DefMagic); return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KStat value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int hp) || !ser.TryGet(out int atkPhysic) || !ser.TryGet(out int atkMagic) ||
                !ser.TryGet(out int defPhysic) || !ser.TryGet(out int defMagic)) return (false, existing);
            existing.BaseHp = hp; existing.AtkPhysic = atkPhysic; existing.AtkMagic = atkMagic; existing.DefPhysic = defPhysic; existing.DefMagic = defMagic;
            return (true, existing);
        });
    }
}

public sealed class KSkillData
{
    public short SkillId { get; set; }
    public byte SkillLevel { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.SkillId); ser.Put(value.SkillLevel); return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KSkillData value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out short skillId) || !ser.TryGet(out byte skillLevel)) return (false, existing);
            existing.SkillId = skillId; existing.SkillLevel = skillLevel;
            return (true, existing);
        });
    }
}
