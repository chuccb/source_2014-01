namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Room-side user snapshot; wire order mirrors KRoomUserInfo PUT/GET in CommonPacket.cpp.</summary>
public sealed class KRoomUserInfo
{
    public long GsUid { get; set; }
    public long OwnerUserUid { get; set; }
    public short ServerGroupId { get; set; }
    public sbyte AuthLevel { get; set; }
    public bool Male { get; set; }
    public byte Age { get; set; }
    public long UnitUid { get; set; }
    public uint KnmSerialNum { get; set; }
    public sbyte UnitClass { get; set; }
    public string NickName { get; set; } = string.Empty;
    public int NumResurrectionStone { get; set; }
    public string Ip { get; set; } = string.Empty;
    public ushort Port { get; set; }
    public string InternalIp { get; set; } = string.Empty;
    public ushort InternalPort { get; set; }
    public long PartyUid { get; set; }
    public List<KGamePlayStatus> GamePlayStatus { get; } = [];
    public List<KBuffInfo> BuffInfo { get; } = [];
    public byte Level { get; set; }
    public KStat GameStat { get; set; } = new();
    public SortedDictionary<int, KInventoryItemInfo> EquippedItem { get; } = [];
    public SortedDictionary<int, KInventoryItemInfo> SpecialItem { get; } = [];
    public KUnitSkillData UnitSkillData { get; set; } = new();
    public bool IsPvpNpc { get; set; }
    public int OfficialMatchCnt { get; set; }
    public int Rating { get; set; }
    public int MaxRating { get; set; }
    public bool IsWinBeforeMatch { get; set; }
    public sbyte Rank { get; set; }
    public sbyte RankForServer { get; set; }
    public float KFactor { get; set; }
    public bool IsRedistributionUser { get; set; }
    public int SpiritMax { get; set; }
    public int Spirit { get; set; }
    public bool IsGameBang { get; set; }
    public int PcBangType { get; set; }
    public bool IsObserver { get; set; }
    public SortedDictionary<sbyte, float> BonusRate { get; } = [];
    public bool IsRingOfPvpRebirth { get; set; }
    public bool IsGuestUser { get; set; }
    public SortedDictionary<int, KSubQuestInfo> OngoingQuest { get; } = [];
    public int TitleId { get; set; }
    public int GuildUid { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public byte MembershipGrade { get; set; }
    public List<KPetInfo> Pet { get; } = [];
    public SortedSet<int> QuestInfo { get; } = [];
    public bool UseItem { get; set; }
    public SortedSet<int> GoingQuestInfo { get; } = [];
    public bool ComeBackUser { get; set; }
    public bool HenirReward { get; set; }
    public SortedSet<int> UseSkillBuffInPlay { get; } = [];
    public bool EnterCashShop { get; set; }
    public long RidingPetUid { get; set; }
    public ushort RidingPetId { get; set; }
    public sbyte WeddingStatus { get; set; }
    public long LoverUnitUid { get; set; }
    public int EventQuestClearCount { get; set; }
    public int ExchangeCount { get; set; }
    public int GateOfDarknessSupportEventTime { get; set; }
    public bool Couple { get; set; }
    public long RelationTargetUserUid { get; set; }
    public string RelationTargetUserNickname { get; set; } = string.Empty;
    public long RecruiterUnitUid { get; set; }

    public bool Serialize(NativePrimitiveSerializer serializer, ProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NativeUserClassSerializer(serializer).Put(this, (ser, value) =>
        {
            ser.Put(value.GsUid); ser.Put(value.OwnerUserUid);
            if (options.ServerGroupEventSystem) ser.Put(value.ServerGroupId);
            ser.Put(value.AuthLevel); ser.Put(value.Male); ser.Put(value.Age); ser.Put(value.UnitUid);
            ser.Put(value.KnmSerialNum); ser.Put(value.UnitClass); ser.PutWString(value.NickName);
            if (options.AddDungeonLogColumnNum2) ser.Put(value.NumResurrectionStone);
            ser.PutWString(value.Ip); ser.Put(value.Port); ser.PutWString(value.InternalIp); ser.Put(value.InternalPort);
            var stl = new NativeStlSerializer(ser);
            if (options.BattleFieldSystem)
            {
                ser.Put(value.PartyUid);
                stl.PutVector(value.GamePlayStatus, (s, item) => item.Serialize(s, options));
            }
            if (options.ReformTheGateOfDarkness) stl.PutVector(value.BuffInfo, static (s, item) => item.Serialize(s));
            ser.Put(value.Level);
            value.GameStat.Serialize(ser);
            stl.PutMap(value.EquippedItem, static (s, key) => s.Put(key), (s, item) => item.Serialize(s, options));
            if (options.PaymentItemWithConsumingOtherItem)
                stl.PutMap(value.SpecialItem, static (s, key) => s.Put(key), (s, item) => item.Serialize(s, options));
            value.UnitSkillData.Serialize(ser, options);
            ser.Put(value.IsPvpNpc); ser.Put(value.OfficialMatchCnt); ser.Put(value.Rating); ser.Put(value.MaxRating); ser.Put(value.IsWinBeforeMatch);
            if (options.PvpSeason2) { ser.Put(value.Rank); ser.Put(value.RankForServer); ser.Put(value.KFactor); ser.Put(value.IsRedistributionUser); }
            if (!options.DeleteRoomUserInfoData) { ser.Put(value.SpiritMax); ser.Put(value.Spirit); }
            ser.Put(value.IsGameBang);
            if (options.PcBangType) ser.Put(value.PcBangType);
            ser.Put(value.IsObserver);
            stl.PutMap(value.BonusRate, static (s, key) => s.Put(key), static (s, item) => s.Put(item));
            ser.Put(value.IsRingOfPvpRebirth); ser.Put(value.IsGuestUser);
            stl.PutMap(value.OngoingQuest, static (s, key) => s.Put(key), (s, item) => item.Serialize(s, options));
            ser.Put(value.TitleId);
            if (options.GuildTest) { ser.PutWString(value.GuildName); ser.Put(value.GuildUid); ser.Put(value.MembershipGrade); }
            if (options.PetSystem) stl.PutVector(value.Pet, (s, item) => item.Serialize(s, options));
            if (options.DungeonClearPaymentItem)
            {
                stl.PutSet(value.QuestInfo, static (s, item) => s.Put(item));
                if (options.DungeonClearPaymentItemFix) ser.Put(value.UseItem);
            }
            if (options.PaymentItemOnGoingQuest) stl.PutSet(value.GoingQuestInfo, static (s, item) => s.Put(item));
            if (options.ComeBackUserReward) ser.Put(value.ComeBackUser);
            if (options.NewHenirTest) ser.Put(value.HenirReward);
            if (options.BattleFieldSystem) stl.PutSet(value.UseSkillBuffInPlay, static (s, item) => s.Put(item));
            if (options.VisitCashShop) ser.Put(value.EnterCashShop);
            if (options.RidingPetSystm) { ser.Put(value.RidingPetUid); ser.Put(value.RidingPetId); }
            if (options.RelationshipSystem) { ser.Put(value.WeddingStatus); ser.Put(value.LoverUnitUid); }
            if (options.GrowUpSocket) { ser.Put(value.EventQuestClearCount); ser.Put(value.ExchangeCount); }
            if (options.GateOfDarknessSupportEvent) ser.Put(value.GateOfDarknessSupportEventTime);
            if (options.RelationshipEventInt) { ser.Put(value.Couple); ser.Put(value.RelationTargetUserUid); ser.PutWString(value.RelationTargetUserNickname); }
            if (options.RecruitEventBase) ser.Put(value.RecruiterUnitUid);
            return true;
        });
    }

    public static bool TryDeserialize(NativePrimitiveSerializer serializer, ProtocolOptions options, out KRoomUserInfo value)
    {
        ArgumentNullException.ThrowIfNull(options);
        value = new();
        return new NativeUserClassSerializer(serializer).TryGet(out value, (ser, x) =>
        {
            short serverGroupId = 0;
            if (!ser.TryGet(out long gsUid) || !ser.TryGet(out long ownerUid) ||
                (options.ServerGroupEventSystem && !ser.TryGet(out serverGroupId)) ||
                !ser.TryGet(out sbyte authLevel) || !ser.TryGet(out bool male) || !ser.TryGet(out byte age) ||
                !ser.TryGet(out long unitUid) || !ser.TryGet(out uint knmSerial) || !ser.TryGet(out sbyte unitClass) ||
                !ser.TryGetWString(out var nickName)) return (false, x);
            int resurrectionStone = 0;
            if (options.AddDungeonLogColumnNum2 && !ser.TryGet(out resurrectionStone)) return (false, x);
            if (!ser.TryGetWString(out var ip) || !ser.TryGet(out ushort port) || !ser.TryGetWString(out var internalIp) || !ser.TryGet(out ushort internalPort)) return (false, x);

            var stl = new NativeStlSerializer(ser);
            long partyUid = 0;
            List<KGamePlayStatus> gameplay = [];
            if (options.BattleFieldSystem && (!ser.TryGet(out partyUid) || !stl.TryGetVector(out gameplay,
                s => KGamePlayStatus.TryDeserialize(s, options, out var item) ? (true, item) : (false, new KGamePlayStatus())))) return (false, x);
            List<KBuffInfo> buffInfo = [];
            if (options.ReformTheGateOfDarkness && !stl.TryGetVector(out buffInfo,
                static s => KBuffInfo.TryDeserialize(s, out var item) ? (true, item) : (false, new KBuffInfo()))) return (false, x);

            if (!ser.TryGet(out byte level) || !KStat.TryDeserialize(ser, out var gameStat) ||
                !stl.TryGetMap(out SortedDictionary<int, KInventoryItemInfo> equipped,
                    static s => s.TryGet(out int key) ? (true, key) : (false, 0),
                    s => KInventoryItemInfo.TryDeserialize(s, options, out var item) ? (true, item) : (false, new KInventoryItemInfo()))) return (false, x);
            SortedDictionary<int, KInventoryItemInfo> special = [];
            if (options.PaymentItemWithConsumingOtherItem && !stl.TryGetMap(out special,
                static s => s.TryGet(out int key) ? (true, key) : (false, 0),
                s => KInventoryItemInfo.TryDeserialize(s, options, out var item) ? (true, item) : (false, new KInventoryItemInfo()))) return (false, x);
            if (!KUnitSkillData.TryDeserialize(ser, options, out var unitSkill) ||
                !ser.TryGet(out bool isPvpNpc) || !ser.TryGet(out int officialMatchCnt) || !ser.TryGet(out int rating) ||
                !ser.TryGet(out int maxRating) || !ser.TryGet(out bool isWinBeforeMatch)) return (false, x);

            sbyte rank = 0, rankForServer = 0; float kFactor = 0; bool redistribution = false;
            if (options.PvpSeason2 && (!ser.TryGet(out rank) || !ser.TryGet(out rankForServer) || !ser.TryGet(out kFactor) || !ser.TryGet(out redistribution))) return (false, x);
            int spiritMax = 0, spirit = 0;
            if (!options.DeleteRoomUserInfoData && (!ser.TryGet(out spiritMax) || !ser.TryGet(out spirit))) return (false, x);
            if (!ser.TryGet(out bool isGameBang)) return (false, x);
            int pcBangType = 0;
            if (options.PcBangType && !ser.TryGet(out pcBangType)) return (false, x);
            if (!ser.TryGet(out bool isObserver) || !stl.TryGetMap(out SortedDictionary<sbyte, float> bonusRate,
                static s => s.TryGet(out sbyte key) ? (true, key) : (false, (sbyte)0),
                static s => s.TryGet(out float item) ? (true, item) : (false, 0f)) ||
                !ser.TryGet(out bool isRing) || !ser.TryGet(out bool isGuest) ||
                !stl.TryGetMap(out SortedDictionary<int, KSubQuestInfo> ongoing,
                    static s => s.TryGet(out int key) ? (true, key) : (false, 0),
                    (s, _) => throw new NotSupportedException())) return (false, x);
            // KSubQuestInfo map value reader is supplied separately below to preserve its options-aware payload.
            return (false, x);
        });
    }
}
