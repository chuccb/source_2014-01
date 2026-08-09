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
        var stl = new NativeStlSerializer(serializer);
        new NativeUserClassSerializer(serializer).Put(this, static (ser, value) =>
        {
            ser.Put(value.MaxHp);
            ser.Put(value.CurHp);
            ser.Put(value.MaxMp);
            ser.Put(value.CurMp);
            ser.Put(value.CurHyperGage);
            ser.Put(value.CurHyperCount);
            ser.Put(value.CharAbilType);
            ser.Put(value.CharAbilCount);
            return true;
        });

        stl.PutMap(SkillCoolTime, static (s, key) => s.Put(key), static (s, value) => s.Put(value));
        stl.PutMap(QuickSlotCoolTime, static (s, key) => s.Put(key), static (s, value) => s.Put(value));
        stl.PutSet(PetMp, static (s, value) => s.Put(value));
        if (options.RidingPetSystm)
            stl.PutMap(RidingPetCoolTime, static (s, key) => s.Put(key), static (s, value) => s.Put(value));
        return true;
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
}

public sealed class KGamePlayStatusContainer
{
    public List<KGamePlayStatus> GamePlayStatus { get; } = [];
}
