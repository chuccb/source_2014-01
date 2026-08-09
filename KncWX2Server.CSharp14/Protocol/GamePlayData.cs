namespace KncWX2Server.CSharp14.Protocol;

public sealed class KGamePlayStatus
{
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
            stl.PutMap(value.SkillCoolTime, PutInt, PutInt);
            stl.PutMap(value.QuickSlotCoolTime, PutInt, PutInt);
            stl.PutSet(value.PetMp, PutInt);

            if (options.RidingPetSystm)
                stl.PutMap(value.RidingPetCoolTime, PutInt, PutInt);

            return true;
        });
    }

    public static bool TryDeserialize(
        NativePrimitiveSerializer serializer,
        ProtocolOptions options,
        out KGamePlayStatus value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            if (!ser.TryGet(out int maxHp) ||
                !ser.TryGet(out int curHp) ||
                !ser.TryGet(out int maxMp) ||
                !ser.TryGet(out int curMp) ||
                !ser.TryGet(out int hyperGage) ||
                !ser.TryGet(out sbyte hyperCount) ||
                !ser.TryGet(out sbyte abilType) ||
                !ser.TryGet(out int abilCount))
            {
                return (false, existing);
            }

            var stl = new NativeStlSerializer(ser);

            if (!stl.TryGetMap(out SortedDictionary<int, int> skillCool, GetInt, GetInt) ||
                !stl.TryGetMap(out SortedDictionary<int, int> quickCool, GetInt, GetInt) ||
                !stl.TryGetSet(out SortedSet<int> petMp, GetInt))
            {
                return (false, existing);
            }

            SortedDictionary<int, int> ridingCool = [];
            if (options.RidingPetSystm &&
                !stl.TryGetMap(out ridingCool, GetInt, GetInt))
            {
                return (false, existing);
            }

            existing.MaxHp = maxHp;
            existing.CurHp = curHp;
            existing.MaxMp = maxMp;
            existing.CurMp = curMp;
            existing.CurHyperGage = hyperGage;
            existing.CurHyperCount = hyperCount;
            existing.CharAbilType = abilType;
            existing.CharAbilCount = abilCount;

            existing.SkillCoolTime.Clear();
            foreach (var pair in skillCool)
                existing.SkillCoolTime[pair.Key] = pair.Value;

            existing.QuickSlotCoolTime.Clear();
            foreach (var pair in quickCool)
                existing.QuickSlotCoolTime[pair.Key] = pair.Value;

            existing.PetMp.Clear();
            foreach (var item in petMp)
                existing.PetMp.Add(item);

            existing.RidingPetCoolTime.Clear();
            foreach (var pair in ridingCool)
                existing.RidingPetCoolTime[pair.Key] = pair.Value;

            return (true, existing);
        });
    }

    private static void PutInt(NativePrimitiveSerializer serializer, int value) => serializer.Put(value);

    private static (bool Ok, int Value) GetInt(NativePrimitiveSerializer serializer)
        => serializer.TryGet(out int value)
            ? (true, value)
            : (false, default);
}

public sealed class KGamePlayStatusContainer
{
    public List<KGamePlayStatus> GamePlayStatus { get; } = [];

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            new NativeStlSerializer(ser).PutVector(
                value.GamePlayStatus,
                (s, item) => item.Serialize(s, options));
            return true;
        });
    }

    public static bool TryDeserialize(
        NativePrimitiveSerializer serializer,
        ProtocolOptions options,
        out KGamePlayStatusContainer value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, existing) =>
        {
            if (!new NativeStlSerializer(ser).TryGetVector(
                    out List<KGamePlayStatus> statuses,
                    s => KGamePlayStatus.TryDeserialize(s, options, out var item)
                        ? (true, item)
                        : (false, new KGamePlayStatus())))
            {
                return (false, existing);
            }

            existing.GamePlayStatus.Clear();
            existing.GamePlayStatus.AddRange(statuses);
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

public sealed class KLastPositionInfo
{
    public int MapId { get; set; }
    public byte LastTouchLineIndex { get; set; }
    public ushort LastPosValue { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer) =>
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.MapId);
            ser.Put(value.LastTouchLineIndex);
            ser.Put(value.LastPosValue);
            return true;
        });

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, out KLastPositionInfo value)
    {
        value = new();

        return new NativeUserClassSerializer(serializer).TryGet(out value, static (ser, existing) =>
        {
            if (!ser.TryGet(out int mapId) ||
                !ser.TryGet(out byte lineIndex) ||
                !ser.TryGet(out ushort pos))
            {
                return (false, existing);
            }

            existing.MapId = mapId;
            existing.LastTouchLineIndex = lineIndex;
            existing.LastPosValue = pos;
            return (true, existing);
        });
    }
}
