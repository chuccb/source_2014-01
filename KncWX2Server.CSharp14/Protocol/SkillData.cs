namespace KncWX2Server.CSharp14.Protocol;

public sealed class KUnitSkillData
{
    private const int EquippedSkillCount = 4;

    public KSkillData[] EquippedSkill { get; } = [new(), new(), new(), new()];
    public KSkillData[] EquippedSkillSlotB { get; } = [new(), new(), new(), new()];
    public string SkillSlotBEndDate { get; set; } = string.Empty;
    public sbyte SkillSlotBExpirationState { get; set; }
    public List<KSkillData> PassiveSkill { get; } = [];
    public List<KSkillData> GuildPassiveSkill { get; } = [];
    public List<int> SkillNote { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new NativeUserClassSerializer(serializer).Put(this, static (ser, value, options) =>
        {
            foreach (var skill in value.EquippedSkill)
                skill.Serialize(ser);

            foreach (var skill in value.EquippedSkillSlotB)
                skill.Serialize(ser);

            ser.PutWString(value.SkillSlotBEndDate);
            ser.Put(value.SkillSlotBExpirationState);

            var stl = new NativeStlSerializer(ser);
            stl.PutVector(value.PassiveSkill, SerializeSkill);

            if (options.GuildSkillTest)
                stl.PutVector(value.GuildPassiveSkill, SerializeSkill);

            if (options.SkillNote)
                stl.PutVector(value.SkillNote, PutInt32);

            return true;
        }, options);
    }

    public static bool TryDeserialize(
        NativePrimitiveSerializer serializer,
        ProtocolOptions options,
        out KUnitSkillData value)
    {
        ArgumentNullException.ThrowIfNull(options);

        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing, options) =>
        {
            for (var i = 0; i < EquippedSkillCount; i++)
            {
                if (!KSkillData.TryDeserialize(ser, out var item))
                    return (false, existing);

                existing.EquippedSkill[i] = item;
            }

            for (var i = 0; i < EquippedSkillCount; i++)
            {
                if (!KSkillData.TryDeserialize(ser, out var item))
                    return (false, existing);

                existing.EquippedSkillSlotB[i] = item;
            }

            if (!ser.TryGetWString(out var endDate) ||
                !ser.TryGet(out sbyte expirationState))
            {
                return (false, existing);
            }

            var stl = new NativeStlSerializer(ser);

            if (!stl.TryGetVector(out List<KSkillData> passive, GetSkill))
                return (false, existing);

            List<KSkillData> guildPassive = [];
            if (options.GuildSkillTest &&
                !stl.TryGetVector(out guildPassive, GetSkill))
            {
                return (false, existing);
            }

            List<int> skillNote = [];
            if (options.SkillNote &&
                !stl.TryGetVector(out skillNote, GetInt32))
            {
                return (false, existing);
            }

            existing.SkillSlotBEndDate = endDate;
            existing.SkillSlotBExpirationState = expirationState;

            existing.PassiveSkill.Clear();
            existing.PassiveSkill.AddRange(passive);

            existing.GuildPassiveSkill.Clear();
            existing.GuildPassiveSkill.AddRange(guildPassive);

            existing.SkillNote.Clear();
            existing.SkillNote.AddRange(skillNote);

            return (true, existing);
        }, options);
    }

    private static void SerializeSkill(
        NativePrimitiveSerializer serializer,
        KSkillData value) => value.Serialize(serializer);

    private static void PutInt32(
        NativePrimitiveSerializer serializer,
        int value) => serializer.Put(value);

    private static (bool Ok, KSkillData Value) GetSkill(
        NativePrimitiveSerializer serializer) =>
        KSkillData.TryDeserialize(serializer, out var value)
            ? (true, value)
            : (false, new KSkillData());

    private static (bool Ok, int Value) GetInt32(
        NativePrimitiveSerializer serializer) =>
        serializer.TryGet(out int value)
            ? (true, value)
            : (false, default);
}
