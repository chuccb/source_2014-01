namespace KncWX2Server.Protocol;

/// <summary>Feature switches that mirror the native KUnitInfo serialization gates.</summary>
public sealed record KUnitInfoWireOptions
{
    public bool PvpNewSystem { get; init; }
    public bool PvpSeason2 { get; init; }
    public bool BattleFieldSystem { get; init; }
    public bool ReformTheGateOfDarkness { get; init; }
    public bool LimitedDungeonPlayTimes { get; init; }
    public bool PcBangType { get; init; }
    public bool TitleDataSize { get; init; }
    public bool GuildTest { get; init; }
    public bool UnitWaitDelete { get; init; }
    public bool AddWarpButton { get; init; }
    public bool GrowUpSocket { get; init; }
    public bool ChinaSpiritEvent { get; init; }
    public bool RecruitEventQuestForNewUser { get; init; }
    public bool NewYearEvent2014 { get; init; }
    public bool GuildSkillTest { get; init; }
    public bool SkillNote { get; init; }
    public bool ExpandSlotIdDataSize { get; init; }
    public bool ItemOptionDataSize { get; init; }
    public bool NewItemSystem201305 { get; init; }
    public bool GoldTicket { get; init; }

    public static KUnitInfoWireOptions Default { get; } = new();
}

public static class KUnitInfoSerializer
{
    public static bool Put(this KSerializer serializer, KStat value) =>
        serializer.Put(value.BaseHp)
        && serializer.Put(value.AtkPhysic)
        && serializer.Put(value.AtkMagic)
        && serializer.Put(value.DefPhysic)
        && serializer.Put(value.DefMagic);

    public static bool Get(this KSerializer serializer, KStat value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!serializer.Get(out int baseHp)
            || !serializer.Get(out int atkPhysic)
            || !serializer.Get(out int atkMagic)
            || !serializer.Get(out int defPhysic)
            || !serializer.Get(out int defMagic))
        {
            return false;
        }

        value.BaseHp = baseHp;
        value.AtkPhysic = atkPhysic;
        value.AtkMagic = atkMagic;
        value.DefPhysic = defPhysic;
        value.DefMagic = defMagic;
        return true;
    }

    public static bool Put(this KSerializer serializer, KSkillData value) =>
        serializer.Put(value.SkillId) && serializer.Put(value.SkillLevel);

    public static bool Get(this KSerializer serializer, KSkillData value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!serializer.Get(out short skillId) || !serializer.Get(out byte skillLevel))
        {
            return false;
        }

        value.SkillId = skillId;
        value.SkillLevel = skillLevel;
        return true;
    }

    public static bool Put(this KSerializer serializer, KUnitSkillData value, KUnitInfoWireOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);

        // ... existing implementation unchanged ...
        return true;
    }

    // ... existing implementation unchanged ...

    private static bool HasExpectedSkillSlots(KUnitSkillData value) =>
        value.EquippedSkill.Length == KUnitSkillData.EquippedSkillSlotCount
        && value.EquippedSkillSlotB.Length == KUnitSkillData.EquippedSkillSlotCount;

    private static bool PutSocketVector(KSerializer serializer, IReadOnlyList<int> values, bool useInt32) =>
        useInt32
            ? serializer.PutVector(values, static (s, value) => s.Put(value))
            : serializer.PutVector(values, static (s, value) => s.Put(checked((short)value)));

    private static bool GetSocketVector(KSerializer serializer, ICollection<int> values, bool useInt32) =>
        useInt32
            ? serializer.GetVector(values, static s => ReadInt(s))
            : serializer.GetVector(values, static s => ReadShort(s) is var result ? (result.Ok, (int)result.Value) : result);

    private static bool PutSlotId(KSerializer serializer, int value, bool expanded) =>
        expanded
            ? serializer.Put(checked((short)value))
            : serializer.Put(checked((sbyte)value));

    private static bool GetSlotId(KSerializer serializer, out int value, bool expanded) =>
        expanded
            ? ReadShortAsInt(serializer, out value)
            : ReadSByteAsInt(serializer, out value);

    private static (bool Ok, int Value) ReadInt(KSerializer serializer)
    {
        var ok = serializer.Get(out int value);
        return (ok, value);
    }

    private static (bool Ok, short Value) ReadShort(KSerializer serializer)
    {
        var ok = serializer.Get(out short value);
        return (ok, value);
    }

    private static (bool Ok, float Value) ReadFloat(KSerializer serializer)
    {
        var ok = serializer.Get(out float value);
        return (ok, value);
    }

    private static bool ReadShortAsInt(KSerializer serializer, out int value)
    {
        var ok = serializer.Get(out short raw);
        value = raw;
        return ok;
    }

    private static bool ReadSByteAsInt(KSerializer serializer, out int value)
    {
        var ok = serializer.Get(out sbyte raw);
        value = raw;
        return ok;
    }

    private static (bool Ok, KSkillData Value) ReadSkill(KSerializer serializer)
    {
        var value = new KSkillData();
        return (serializer.Get(value), value);
    }

    // ... remaining existing implementation unchanged ...
}
