namespace KncWX2Server.Protocol;

/// <summary>Native KStat: the five base combat-stat integers serialized in declaration order.</summary>
public sealed class KStat
{
    public int BaseHp { get; set; }
    public int AtkPhysic { get; set; }
    public int AtkMagic { get; set; }
    public int DefPhysic { get; set; }
    public int DefMagic { get; set; }

    public void Add(KStat other)
    {
        ArgumentNullException.ThrowIfNull(other);
        BaseHp += other.BaseHp;
        AtkPhysic += other.AtkPhysic;
        AtkMagic += other.AtkMagic;
        DefPhysic += other.DefPhysic;
        DefMagic += other.DefMagic;
    }

    public KStat Scale(float factor) => new()
    {
        BaseHp = (int)(BaseHp * factor),
        AtkPhysic = (int)(AtkPhysic * factor),
        AtkMagic = (int)(AtkMagic * factor),
        DefPhysic = (int)(DefPhysic * factor),
        DefMagic = (int)(DefMagic * factor),
    };
}

/// <summary>Native KSkillData: short skill id followed by one-byte skill level.</summary>
public sealed class KSkillData
{
    public short SkillId { get; set; }
    public byte SkillLevel { get; set; }
}

/// <summary>
/// Native KUnitSkillData. The two equipped arrays contain exactly four entries each;
/// passive skill collections retain native vector ordering.
/// </summary>
public sealed class KUnitSkillData
{
    public const int EquippedSkillSlotCount = 4;

    public KSkillData[] EquippedSkill { get; } = CreateSkillSlots();
    public KSkillData[] EquippedSkillSlotB { get; } = CreateSkillSlots();
    public string SkillSlotBEndDate { get; set; } = string.Empty;
    public sbyte SkillSlotBExpirationState { get; set; }
    public List<KSkillData> PassiveSkill { get; } = [];
    public List<KSkillData> GuildPassiveSkill { get; } = [];
    public List<int> SkillNote { get; } = [];

    private static KSkillData[] CreateSkillSlots() =>
        [new(), new(), new(), new()];
}

/// <summary>Native KDungeonClearInfo used as the value of KUnitInfo's dungeon-clear map.</summary>
public sealed class KDungeonClearInfo
{
    public int DungeonId { get; set; }
    public int MaxScore { get; set; }
    public sbyte MaxTotalRank { get; set; }
    public string ClearTime { get; set; } = string.Empty;
    public bool IsNew { get; set; }
}

/// <summary>Native KTCClearInfo used as the value of KUnitInfo's training-center map.</summary>
public sealed class KTCClearInfo
{
    public int TcId { get; set; }
    public string ClearTime { get; set; } = string.Empty;
    public bool IsNew { get; set; }
}

/// <summary>Native KDungeonPlayInfo used when SERV_LIMITED_DUNGEON_PLAY_TIMES is enabled.</summary>
public sealed class KDungeonPlayInfo
{
    public int DungeonId { get; set; }
    public int PlayTimes { get; set; }
    public int ClearTimes { get; set; }
    public bool IsNew { get; set; }
}

/// <summary>Native KLastPositionInfo used by SERV_BATTLE_FIELD_SYSTEM.</summary>
public sealed class KLastPositionInfo
{
    public int MapId { get; set; }
    public byte LastTouchLineIndex { get; set; }
    public ushort LastPosValue { get; set; }
}

/// <summary>Native KUserGuildInfo, included only when GUILD_TEST is enabled.</summary>
public sealed class KUserGuildInfo
{
    public int GuildUid { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public byte MembershipGrade { get; set; }
    public int HonorPoint { get; set; }
}
