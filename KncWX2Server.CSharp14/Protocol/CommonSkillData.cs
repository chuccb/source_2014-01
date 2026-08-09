namespace KncWX2Server.CSharp14.Protocol;

public sealed class KUnitSkillData
{
    public const int EquippedSkillSlotCount = 4;

    public KSkillData[] EquippedSkill { get; } =
        [new(), new(), new(), new()];

    public KSkillData[] EquippedSkillSlotB { get; } =
        [new(), new(), new(), new()];

    public string SkillSlotBEndDate { get; set; } = string.Empty;
    public sbyte SkillSlotBExpirationState { get; set; }
    public List<KSkillData> PassiveSkill { get; set; } = [];
    public List<KSkillData> GuildPassiveSkill { get; set; } = [];
    public List<int> SkillNote { get; set; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            foreach (var skill in value.EquippedSkill)
                skill.Serialize(ser);
            foreach (var skill in value.EquippedSkillSlotB)
                skill.Serialize(ser);

            ser.PutWString(value.SkillSlotBEndDate);
            ser.Put(value.SkillSlotBExpirationState);

            var stl = new NativeStlSerializer(ser);
            stl.PutVector(value.PassiveSkill, static (s, item) => item.Serialize(s));

            if (options.GuildSkillTest)
                stl.PutVector(value.GuildPassiveSkill, static (s, item) => item.Serialize(s));
            if (options.SkillNote)
                stl.PutVector(value.SkillNote, static (s, item) => s.Put(item));

            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KUnitSkillData value,
        ProtocolOptions? options = null)
    {
        options ??= ProtocolOptions.Default;
        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            for (var i = 0; i < EquippedSkillSlotCount; i++)
            {
                if (!KSkillData.TryDeserialize(ser, out var equipped) ||
                    !KSkillData.TryDeserialize(ser, out var slotB))
                    return (false, existing);

                existing.EquippedSkill[i] = equipped;
                existing.EquippedSkillSlotB[i] = slotB;
            }

            if (!ser.TryGetWString(out var endDate) ||
                !ser.TryGet(out sbyte expirationState))
                return (false, existing);

            var stl = new NativeStlSerializer(ser);
            if (!stl.TryGetVector(out List<KSkillData> passive,
                static s => KSkillData.TryDeserialize(s, out var item)
                    ? (true, item)
                    : (false, default!)))
                return (false, existing);

            existing.SkillSlotBEndDate = endDate;
            existing.SkillSlotBExpirationState = expirationState;
            existing.PassiveSkill = passive;

            if (options.GuildSkillTest)
            {
                if (!stl.TryGetVector(out List<KSkillData> guildPassive,
                    static s => KSkillData.TryDeserialize(s, out var item)
                        ? (true, item)
                        : (false, default!)))
                    return (false, existing);

                existing.GuildPassiveSkill = guildPassive;
            }

            if (options.SkillNote)
            {
                if (!stl.TryGetVector(out List<int> skillNote,
                    static s => s.TryGet(out int item)
                        ? (true, item)
                        : (false, default)))
                    return (false, existing);

                existing.SkillNote = skillNote;
            }

            return (true, existing);
        });
    }
}
