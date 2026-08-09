namespace KncWX2Server.CSharp14.Protocol;

public sealed class KUnitSkillData
{
    public KSkillData[] EquippedSkill { get; } = new KSkillData[4];
    public KSkillData[] EquippedSkillSlotB { get; } = new KSkillData[4];
    public string SkillSlotBEndDate { get; set; } = string.Empty;
    public sbyte SkillSlotBExpirationState { get; set; }
    public List<KSkillData> PassiveSkill { get; } = [];
    public List<KSkillData> GuildPassiveSkill { get; } = [];
    public List<int> SkillNote { get; } = [];

    public KUnitSkillData()
    {
        for (var i = 0; i < 4; i++)
        {
            EquippedSkill[i] = new();
            EquippedSkillSlotB[i] = new();
        }
    }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            for (var i = 0; i < 4; i++) value.EquippedSkill[i].Serialize(ser);
            for (var i = 0; i < 4; i++) value.EquippedSkillSlotB[i].Serialize(ser);
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

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KUnitSkillData value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            for (var i = 0; i < 4; i++)
                if (!KSkillData.TryDeserialize(ser, out existing.EquippedSkill[i])) return (false, existing);
            for (var i = 0; i < 4; i++)
                if (!KSkillData.TryDeserialize(ser, out existing.EquippedSkillSlotB[i])) return (false, existing);
            if (!ser.TryGetWString(out var endDate) || !ser.TryGet(out sbyte expirationState)) return (false, existing);
            var stl = new NativeStlSerializer(ser);
            if (!stl.TryGetVector(out List<KSkillData> passive,
                    static s => KSkillData.TryDeserialize(s, out var item) ? (true, item) : (false, new KSkillData())))
                return (false, existing);

            List<KSkillData> guildPassive = [];
            if (options.GuildSkillTest && !stl.TryGetVector(out guildPassive,
                    static s => KSkillData.TryDeserialize(s, out var item) ? (true, item) : (false, new KSkillData())))
                return (false, existing);

            List<int> skillNote = [];
            if (options.SkillNote && !stl.TryGetVector(out skillNote,
                    static s => s.TryGet(out int item) ? (true, item) : (false, 0)))
                return (false, existing);

            existing.SkillSlotBEndDate = endDate;
            existing.SkillSlotBExpirationState = expirationState;
            existing.PassiveSkill.Clear(); existing.PassiveSkill.AddRange(passive);
            existing.GuildPassiveSkill.Clear(); existing.GuildPassiveSkill.AddRange(guildPassive);
            existing.SkillNote.Clear(); existing.SkillNote.AddRange(skillNote);
            return (true, existing);
        });
    }
}
