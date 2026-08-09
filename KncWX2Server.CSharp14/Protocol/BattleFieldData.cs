namespace KncWX2Server.CSharp14.Protocol;

public sealed class KGamePlayStatus
{
    public const sbyte CharAbilNone = 0;
    public const sbyte CharAbilWsp = 1;
    public const sbyte CharAbilCannonBallCount = 2;
    public const sbyte CharAbilForcePower = 3;

    public int MaxHp { get; set; }
    public int CurHp { get; set; }
    public int MaxMp { get; set; }
    public int CurMp { get; set; }
    public int CurHyperGage { get; set; }
    public sbyte CurHyperCount { get; set; }
    public sbyte CharAbilType { get; set; }
    public int CharAbilCount { get; set; }
    public SortedDictionary<int, int> SkillCoolTime { get; } = [];
    public SortedDictionary<int, int> QuickSlotCoolTime { get; } = [];
    public SortedSet<int> PetMp { get; } = [];
    public SortedDictionary<int, int> RidingPetCoolTime { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.MaxHp);
            ser.Put(value.CurHp);
            ser.Put(value.MaxMp);
            ser.Put(value.CurMp);
            ser.Put(value.CurHyperGage);
            ser.Put(value.CurHyperCount);
            ser.Put(value.CharAbilType);
            ser.Put(value.CharAbilCount);

            var stl = new NativeStlSerializer(ser);
            stl.PutMap(value.SkillCoolTime, static (s, key) => s.Put(key), static (s, item) => s.Put(item));
            stl.PutMap(value.QuickSlotCoolTime, static (s, key) => s.Put(key), static (s, item) => s.Put(item));
            stl.PutSet(value.PetMp, static (s, item) => s.Put(item));
            if (options.RidingPetSystm)
                stl.PutMap(value.RidingPetCoolTime, static (s, key) => s.Put(key), static (s, item) => s.Put(item));
            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KGamePlayStatus value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            if (!ser.TryGet(out int maxHp) || !ser.TryGet(out int curHp) || !ser.TryGet(out int maxMp) ||
                !ser.TryGet(out int curMp) || !ser.TryGet(out int hyperGage) ||
                !ser.TryGet(out sbyte hyperCount) || !ser.TryGet(out sbyte abilType) ||
                !ser.TryGet(out int abilCount))
                return (false, existing);

            var stl = new NativeStlSerializer(ser);
            if (!stl.TryGetMap(out var skillCoolTime,
                    static s => s.TryGet(out int v) ? (true, v) : (false, 0),
                    static s => s.TryGet(out int v) ? (true, v) : (false, 0)))
                return (false, existing);
            if (!stl.TryGetMap(out var quickSlotCoolTime,
                    static s => s.TryGet(out int v) ? (true, v) : (false, 0),
                    static s => s.TryGet(out int v) ? (true, v) : (false, 0)))
                return (false, existing);
            if (!stl.TryGetSet(out var petMp,
                    static s => s.TryGet(out int v) ? (true, v) : (false, 0)))
                return (false, existing);

            SortedDictionary<int, int> ridingPetCoolTime = [];
            if (options.RidingPetSystm && !stl.TryGetMap(out ridingPetCoolTime,
                    static s => s.TryGet(out int v) ? (true, v) : (false, 0),
                    static s => s.TryGet(out int v) ? (true, v) : (false, 0)))
                return (false, existing);

            existing.MaxHp = maxHp;
            existing.CurHp = curHp;
            existing.MaxMp = maxMp;
            existing.CurMp = curMp;
            existing.CurHyperGage = hyperGage;
            existing.CurHyperCount = hyperCount;
            existing.CharAbilType = abilType;
            existing.CharAbilCount = abilCount;
            existing.SkillCoolTime.Clear();
            foreach (var pair in skillCoolTime) existing.SkillCoolTime[pair.Key] = pair.Value;
            existing.QuickSlotCoolTime.Clear();
            foreach (var pair in quickSlotCoolTime) existing.QuickSlotCoolTime[pair.Key] = pair.Value;
            existing.PetMp.Clear();
            foreach (var item in petMp) existing.PetMp.Add(item);
            existing.RidingPetCoolTime.Clear();
            foreach (var pair in ridingPetCoolTime) existing.RidingPetCoolTime[pair.Key] = pair.Value;
            return (true, existing);
        });
    }
}

public sealed class KPartyMemberStatus
{
    public float HpPercent { get; set; }
    public float MpPercent { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.HpPercent);
            ser.Put(value.MpPercent);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KPartyMemberStatus value)
    {
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out float hp) || !ser.TryGet(out float mp))
                return (false, existing);
            existing.HpPercent = hp;
            existing.MpPercent = mp;
            return (true, existing);
        });
    }
}

public sealed class KGamePlayStatusContainer
{
    public List<KGamePlayStatus> GamePlayStatus { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            new NativeStlSerializer(ser).PutVector(value.GamePlayStatus,
                (s, item) => item.Serialize(s, options));
            return true;
        });
    }
}
