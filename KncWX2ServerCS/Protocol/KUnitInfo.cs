namespace KncWX2Server.Protocol;

/// <summary>C# representation of native KUnitInfo.</summary>
public sealed class KUnitInfo
{
    public long OwnerUserUid { get; set; }
    public sbyte AuthLevel { get; set; }
    public long UnitUid { get; set; }
    public uint KnmSerialNumber { get; set; }
    public sbyte UnitClass { get; set; }
    public string NickName { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public ushort Port { get; set; }

    public int Ed { get; set; }
    public byte Level { get; set; }
    public int Exp { get; set; }

    public int OfficialMatchCount { get; set; }
    public int Rating { get; set; }
    public int MaxRating { get; set; }
    public int RPoint { get; set; }
    public int APoint { get; set; }
    public bool IsWinBeforeMatch { get; set; }
    public sbyte Rank { get; set; }
    public float KFactor { get; set; }
    public bool IsRedistributionUser { get; set; }
    public int PastSeasonWin { get; set; }

    public int PvpEmblem { get; set; }
    public int VsPoint { get; set; }
    public int VsPointMax { get; set; }

    public int SPoint { get; set; }
    public int CsPoint { get; set; }
    public int MaxCsPoint { get; set; }
    public string CsPointEndDate { get; set; } = string.Empty;
    public int NowBaseLevelExp { get; set; }
    public int NextBaseLevelExp { get; set; }
    public int StraightVictories { get; set; }

    public KStat Stat { get; } = new();
    public KStat GameStat { get; } = new();

    public KLastPositionInfo LastPosition { get; } = new();
    public List<KBuffInfo> BuffInfo { get; } = [];
    public int LegacyMapId { get; set; }
    public byte LegacyLastTouchLineIndex { get; set; }
    public ushort LegacyLastPosValue { get; set; }

    public int Win { get; set; }
    public int Lose { get; set; }

    public SortedDictionary<int, KDungeonClearInfo> DungeonClear { get; } = [];
    public SortedDictionary<int, KTCClearInfo> TCClear { get; } = [];
    public SortedDictionary<int, KDungeonPlayInfo> DungeonPlay { get; } = [];
    public SortedDictionary<int, KInventoryItemInfo> EquippedItem { get; } = [];

    public KUnitSkillData UnitSkillData { get; } = new();

    public bool IsParty { get; set; }
    public int SpiritMax { get; set; }
    public int Spirit { get; set; }
    public bool IsGameBang { get; set; }
    public int PcBangType { get; set; } = -1;
    public int TitleId { get; set; }
    public short LegacyTitleId { get; set; }

    public KUserGuildInfo UserGuildInfo { get; } = new();

    public string LastDate { get; set; } = string.Empty;
    public bool Deleted { get; set; }
    public long DeleteAvailableDate { get; set; }
    public long RestoreAvailableDate { get; set; }

    public long WarpVipEndDate { get; set; }
    public int EventQuestClearCount { get; set; }
    public int ExchangeCount { get; set; }
    public int[] ChinaSpirit { get; } = new int[6];
    public bool Recruit { get; set; }
    public byte OldYearMissionRewardedLevel { get; set; }
    public int NewYearMissionStepId { get; set; } = -1;
}