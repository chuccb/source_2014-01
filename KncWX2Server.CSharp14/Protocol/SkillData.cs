namespace KncWX2Server.CSharp14.Protocol;

public sealed class KUnitSkillData
{
    private const int EquippedSkillSlotCount = 4;

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

        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            SerializeSkills(ser, value.EquippedSkill);
            SerializeSkills(ser, value.EquippedSkillSlotB);

            ser.PutWString(value.SkillSlotBEndDate);
            ser.Put(value.SkillSlotBExpirationState);

            var stl = new NativeStlSerializer(ser);
            stl.PutVector(value.PassiveSkill, SerializeSkill);

            if (options.GuildSkillTest)
                stl.PutVector(value.GuildPassiveSkill, SerializeSkill);

            if (options.SkillNote)
                stl.PutVector(value.SkillNote, PutInt32);

            return true;
        });
    }

    public static bool TryDeserialize(
        NativePrimitiveSerializer serializer,
        ProtocolOptions options,
        out KUnitSkillData value)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            if (!TryDeserializeSkills(ser, out var equippedSkill) ||
                !TryDeserializeSkills(ser, out var equippedSkillSlotB))
            {
                return (false, existing);
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

            equippedSkill.CopyTo(existing.EquippedSkill, 0);
            equippedSkillSlotB.CopyTo(existing.EquippedSkillSlotB, 0);
            existing.SkillSlotBEndDate = endDate;
            existing.SkillSlotBExpirationState = expirationState;

            existing.PassiveSkill.Clear();
            existing.PassiveSkill.AddRange(passive);

            existing.GuildPassiveSkill.Clear();
            existing.GuildPassiveSkill.AddRange(guildPassive);

            existing.SkillNote.Clear();
            existing.SkillNote.AddRange(skillNote);

            return (true, existing);
        });
    }

    private static void SerializeSkills(
        NativePrimitiveSerializer serializer,
        IReadOnlyList<KSkillData> skills)
    {
        foreach (var skill in skills)
            skill.Serialize(serializer);
    }

    private static bool TryDeserializeSkills(
        NativePrimitiveSerializer serializer,
        out KSkillData[] skills)
    {
        var parsedSkills = new KSkillData[EquippedSkillSlotCount];

        for (var i = 0; i < parsedSkills.Length; i++)
        {
            if (!KSkillData.TryDeserialize(serializer, out var skill))
            {
                skills = [];
                return false;
            }

            parsedSkills[i] = skill;
        }

        skills = parsedSkills;
        return true;
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
            : (false, default!);

    private static (bool Ok, int Value) GetInt32(
        NativePrimitiveSerializer serializer) =>
        serializer.TryGet(out int value)
            ? (true, value)
            : (false, default);
}