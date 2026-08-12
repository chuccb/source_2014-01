namespace KncWX2Server.Protocol;

/// <summary>Feature switches that mirror the native KUnitInfo serialization gates.</summary>
public sealed record KUnitInfoWireOptions
{
    public bool PvpNewSystem { get; init; }
    public bool PvpSeason2 { get; init; }
    public bool BattleFieldSystem { get; init; }
    public bool ReformTheGateOfDarkness { get; init; }
    public bool LimitedDungeonPlayTimes { get; init; }
    public bool PcBangType { get; init; }
    public bool TitleDataSize { get; init; }
    public bool GuildTest { get; init; }
    public bool UnitWaitDelete { get; init; }
    public bool AddWarpButton { get; init; }
    public bool GrowUpSocket { get; init; }
    public bool ChinaSpiritEvent { get; init; }
    public bool RecruitEventQuestForNewUser { get; init; }
    public bool NewYearEvent2014 { get; init; }
    public bool GuildSkillTest { get; init; }
    public bool SkillNote { get; init; }
    public bool ExpandSlotIdDataSize { get; init; }
    public bool ItemOptionDataSize { get; init; }
    public bool NewItemSystem201305 { get; init; }
    public bool GoldTicket { get; init; }
    public static KUnitInfoWireOptions Default { get; } = new();
}

public static class KUnitInfoSerializer
{
    public static bool Put(this KSerializer serializer, KStat value) => serializer.Put(value.BaseHp) && serializer.Put(value.AtkPhysic) && serializer.Put(value.AtkMagic) && serializer.Put(value.DefPhysic) && serializer.Put(value.DefMagic);
    public static bool Get(this KSerializer serializer, KStat value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!serializer.Get(out int baseHp) || !serializer.Get(out int atkPhysic) || !serializer.Get(out int atkMagic) || !serializer.Get(out int defPhysic) || !serializer.Get(out int defMagic)) return false;
        value.BaseHp = baseHp; value.AtkPhysic = atkPhysic; value.AtkMagic = atkMagic; value.DefPhysic = defPhysic; value.DefMagic = defMagic; return true;
    }
    public static bool Put(this KSerializer serializer, KSkillData value) => serializer.Put(value.SkillId) && serializer.Put(value.SkillLevel);
    public static bool Get(this KSerializer serializer, KSkillData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!serializer.Get(out short skillId) || !serializer.Get(out byte skillLevel)) return false;
        value.SkillId = skillId; value.SkillLevel = skillLevel; return true;
    }

    public static bool Put(this KSerializer serializer, KUnitSkillData value, KUnitInfoWireOptions options)
    {
        ArgumentNullException.ThrowIfNull(value); ArgumentNullException.ThrowIfNull(options);
        if (!HasExpectedSkillSlots(value)) return false;
        foreach (var skill in value.EquippedSkill) if (!serializer.Put(skill)) return false;
        foreach (var skill in value.EquippedSkillSlotB) if (!serializer.Put(skill)) return false;
        if (!serializer.PutW(value.SkillSlotBEndDate) || !serializer.Put(value.SkillSlotBExpirationState) || !serializer.PutVector(value.PassiveSkill, static (s, item) => s.Put(item))) return false;
        if (options.GuildSkillTest && !serializer.PutVector(value.GuildPassiveSkill, static (s, item) => s.Put(item))) return false;
        return !options.SkillNote || serializer.PutVector(value.SkillNote, static (s, item) => s.Put(item));
    }
    public static bool Get(this KSerializer serializer, KUnitSkillData value, KUnitInfoWireOptions options)
    {
        ArgumentNullException.ThrowIfNull(value); ArgumentNullException.ThrowIfNull(options);
        if (!HasExpectedSkillSlots(value)) return false;
        foreach (var skill in value.EquippedSkill) if (!serializer.Get(skill)) return false;
        foreach (var skill in value.EquippedSkillSlotB) if (!serializer.Get(skill)) return false;
        if (!serializer.GetW(out var endDate) || !serializer.Get(out sbyte expirationState) || !serializer.GetVector(value.PassiveSkill, static s => ReadSkill(s))) return false;
        value.SkillSlotBEndDate = endDate; value.SkillSlotBExpirationState = expirationState;
        if (options.GuildSkillTest && !serializer.GetVector(value.GuildPassiveSkill, static s => ReadSkill(s))) return false;
        return !options.SkillNote || serializer.GetVector(value.SkillNote, static s => ReadInt(s));
    }

    public static bool Put(this KSerializer serializer, KBuffBehaviorFactor value) => serializer.Put(value.Type) && serializer.PutVector(value.Values, static (s, item) => s.Put(item));
    public static bool Get(this KSerializer serializer, KBuffBehaviorFactor value)
    {
        ArgumentNullException.ThrowIfNull(value); if (!serializer.Get(out uint type) || !serializer.GetVector(value.Values, static s => ReadFloat(s))) return false; value.Type = type; return true;
    }
    public static bool Put(this KSerializer serializer, KBuffFinalizerFactor value) => serializer.Put(value.Type) && serializer.PutVector(value.Values, static (s, item) => s.Put(item));
    public static bool Get(this KSerializer serializer, KBuffFinalizerFactor value)
    {
        ArgumentNullException.ThrowIfNull(value); if (!serializer.Get(out uint type) || !serializer.GetVector(value.Values, static s => ReadFloat(s))) return false; value.Type = type; return true;
    }
    public static bool Put(this KSerializer serializer, KBuffIdentity value) => serializer.Put(value.BuffTempletId) && serializer.Put(value.UniqueNumber);
    public static bool Get(this KSerializer serializer, KBuffIdentity value)
    {
        ArgumentNullException.ThrowIfNull(value); if (!serializer.Get(out int buffTempletId) || !serializer.Get(out uint uniqueNumber)) return false; value.BuffTempletId = buffTempletId; value.UniqueNumber = uniqueNumber; return true;
    }
    public static bool Put(this KSerializer serializer, KBuffFactor value) => serializer.PutVector(value.BehaviorFactors, static (s, item) => s.Put(item)) && serializer.PutVector(value.FinalizerFactors, static (s, item) => s.Put(item)) && serializer.Put(value.BuffIdentity) && serializer.Put(value.MessageGameUnitUid) && serializer.Put(value.AccumulationMultiplier) && serializer.Put(value.AccumulationCountNow) && serializer.Put(value.IsMessageGameUnitNpc) && serializer.Put(value.FactorId);
    public static bool Get(this KSerializer serializer, KBuffFactor value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!serializer.GetVector(value.BehaviorFactors, static s => ReadBehaviorFactor(s)) || !serializer.GetVector(value.FinalizerFactors, static s => ReadFinalizerFactor(s)) || !serializer.Get(value.BuffIdentity) || !serializer.Get(out long messageGameUnitUid) || !serializer.Get(out float accumulationMultiplier) || !serializer.Get(out byte accumulationCountNow) || !serializer.Get(out bool isMessageGameUnitNpc) || !serializer.Get(out int factorId)) return false;
        value.MessageGameUnitUid = messageGameUnitUid; value.AccumulationMultiplier = accumulationMultiplier; value.AccumulationCountNow = accumulationCountNow; value.IsMessageGameUnitNpc = isMessageGameUnitNpc; value.FactorId = factorId; return true;
    }
    public static bool Put(this KSerializer serializer, KBuffInfo value) => serializer.Put(value.FactorInfo) && serializer.Put(value.BuffStartTime) && serializer.Put(value.BuffEndTime);
    public static bool Get(this KSerializer serializer, KBuffInfo value)
    {
        ArgumentNullException.ThrowIfNull(value); if (!serializer.Get(value.FactorInfo) || !serializer.Get(out long buffStartTime) || !serializer.Get(out long buffEndTime)) return false; value.BuffStartTime = buffStartTime; value.BuffEndTime = buffEndTime; return true;
    }
    public static bool Put(this KSerializer serializer, KDungeonClearInfo value) => serializer.Put(value.DungeonId) && serializer.Put(value.MaxScore) && serializer.Put(value.MaxTotalRank) && serializer.PutW(value.ClearTime) && serializer.Put(value.IsNew);
    public static bool Get(this KSerializer serializer, KDungeonClearInfo value)
    {
        ArgumentNullException.ThrowIfNull(value); if (!serializer.Get(out int dungeonId) || !serializer.Get(out int maxScore) || !serializer.Get(out sbyte maxTotalRank) || !serializer.GetW(out var clearTime) || !serializer.Get(out bool isNew)) return false; value.DungeonId = dungeonId; value.MaxScore = maxScore; value.MaxTotalRank = maxTotalRank; value.ClearTime = clearTime; value.IsNew = isNew; return true;
    }
    public static bool Put(this KSerializer serializer, KTCClearInfo value) => serializer.Put(value.TcId) && serializer.PutW(value.ClearTime) && serializer.Put(value.IsNew);
    public static bool Get(this KSerializer serializer, KTCClearInfo value)
    {
        ArgumentNullException.ThrowIfNull(value); if (!serializer.Get(out int tcId) || !serializer.GetW(out var clearTime) || !serializer.Get(out bool isNew)) return false; value.TcId = tcId; value.ClearTime = clearTime; value.IsNew = isNew; return true;
    }
    public static bool Put(this KSerializer serializer, KDungeonPlayInfo value) => serializer.Put(value.DungeonId) && serializer.Put(value.PlayTimes) && serializer.Put(value.ClearTimes) && serializer.Put(value.IsNew);
    public static bool Get(this KSerializer serializer, KDungeonPlayInfo value)
    {
        ArgumentNullException.ThrowIfNull(value); if (!serializer.Get(out int dungeonId) || !serializer.Get(out int playTimes) || !serializer.Get(out int clearTimes) || !serializer.Get(out bool isNew)) return false; value.DungeonId = dungeonId; value.PlayTimes = playTimes; value.ClearTimes = clearTimes; value.IsNew = isNew; return true;
    }
    public static bool Put(this KSerializer serializer, KLastPositionInfo value) => serializer.Put(value.MapId) && serializer.Put(value.LastTouchLineIndex) && serializer.Put(value.LastPosValue);
    public static bool Get(this KSerializer serializer, KLastPositionInfo value)
    {
        ArgumentNullException.ThrowIfNull(value); if (!serializer.Get(out int mapId) || !serializer.Get(out byte lineIndex) || !serializer.Get(out ushort posValue)) return false; value.MapId = mapId; value.LastTouchLineIndex = lineIndex; value.LastPosValue = posValue; return true;
    }
    public static bool Put(this KSerializer serializer, KItemAttributeEnchantInfo value) => serializer.Put(value.AttribEnchant0) && serializer.Put(value.AttribEnchant1) && serializer.Put(value.AttribEnchant2);
    public static bool Get(this KSerializer serializer, KItemAttributeEnchantInfo value)
    {
        ArgumentNullException.ThrowIfNull(value); if (!serializer.Get(out sbyte first) || !serializer.Get(out sbyte second) || !serializer.Get(out sbyte third)) return false; value.AttribEnchant0 = first; value.AttribEnchant1 = second; value.AttribEnchant2 = third; return true;
    }
    public static bool Put(this KSerializer serializer, KItemInfo value, KUnitInfoWireOptions options)
    {
        ArgumentNullException.ThrowIfNull(value); ArgumentNullException.ThrowIfNull(options); if (!serializer.Put(value.ItemId) || !serializer.Put(value.UsageType) || !serializer.Put(value.Quantity) || !serializer.Put(value.Endurance) || !serializer.Put(value.SealData) || !serializer.Put(value.EnchantLevel) || !serializer.Put(value.AttributeEnchantInfo) || !PutSocketVector(serializer, value.ItemSocket, options.ItemOptionDataSize)) return false; if (options.NewItemSystem201305 && (!PutSocketVector(serializer, value.RandomSocket, options.ItemOptionDataSize) || !serializer.Put(value.ItemState))) return false; if (!serializer.Put(value.Period) || !serializer.PutW(value.ExpirationDate)) return false; return !options.GoldTicket || serializer.Put(value.GoldTicketKeyUid);
    }
    public static bool Get(this KSerializer serializer, KItemInfo value, KUnitInfoWireOptions options)
    {
        ArgumentNullException.ThrowIfNull(value); ArgumentNullException.ThrowIfNull(options); if (!serializer.Get(out int itemId) || !serializer.Get(out sbyte usageType) || !serializer.Get(out int quantity) || !serializer.Get(out short endurance) || !serializer.Get(out byte sealData) || !serializer.Get(out sbyte enchantLevel) || !serializer.Get(value.AttributeEnchantInfo) || !GetSocketVector(serializer, value.ItemSocket, options.ItemOptionDataSize)) return false; value.ItemId = itemId; value.UsageType = usageType; value.Quantity = quantity; value.Endurance = endurance; value.SealData = sealData; value.EnchantLevel = enchantLevel; if (options.NewItemSystem201305) { if (!GetSocketVector(serializer, value.RandomSocket, options.ItemOptionDataSize) || !serializer.Get(out sbyte itemState)) return false; value.ItemState = itemState; } if (!serializer.Get(out short period) || !serializer.GetW(out var expirationDate)) return false; value.Period = period; value.ExpirationDate = expirationDate; if (options.GoldTicket) { if (!serializer.Get(out long goldTicketKeyUid)) return false; value.GoldTicketKeyUid = goldTicketKeyUid; } return true;
    }
    public static bool Put(this KSerializer serializer, KInventoryItemInfo value, KUnitInfoWireOptions options) => serializer.Put(value.ItemUid) && serializer.Put(value.SlotCategory) && PutSlotId(serializer, value.SlotId, options.ExpandSlotIdDataSize) && serializer.Put(value.ItemInfo, options);
    public static bool Get(this KSerializer serializer, KInventoryItemInfo value, KUnitInfoWireOptions options)
    {
        ArgumentNullException.ThrowIfNull(value); ArgumentNullException.ThrowIfNull(options); if (!serializer.Get(out long itemUid) || !serializer.Get(out sbyte slotCategory) || !GetSlotId(serializer, out int slotId, options.ExpandSlotIdDataSize) || !serializer.Get(value.ItemInfo, options)) return false; value.ItemUid = itemUid; value.SlotCategory = slotCategory; value.SlotId = slotId; return true;
    }
    public static bool Put(this KSerializer serializer, KUserGuildInfo value) => serializer.Put(value.GuildUid) && serializer.PutW(value.GuildName) && serializer.Put(value.MembershipGrade) && serializer.Put(value.HonorPoint);
    public static bool Get(this KSerializer serializer, KUserGuildInfo value)
    {
        ArgumentNullException.ThrowIfNull(value); if (!serializer.Get(out int guildUid) || !serializer.GetW(out var guildName) || !serializer.Get(out byte membershipGrade) || !serializer.Get(out int honorPoint)) return false; value.GuildUid = guildUid; value.GuildName = guildName; value.MembershipGrade = membershipGrade; value.HonorPoint = honorPoint; return true;
    }

    public static bool Put(this KSerializer serializer, KUnitInfo value, KUnitInfoWireOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value); options ??= KUnitInfoWireOptions.Default;
        if (!PutBase(serializer, value) || !PutPvp(serializer, value, options) || !serializer.Put(value.SPoint) || !serializer.Put(value.CsPoint) || !serializer.Put(value.MaxCsPoint) || !serializer.PutW(value.CsPointEndDate) || !serializer.Put(value.NowBaseLevelExp) || !serializer.Put(value.NextBaseLevelExp) || !serializer.Put(value.StraightVictories) || !serializer.Put(value.Stat) || !serializer.Put(value.GameStat) || !PutPosition(serializer, value, options)) return false;
        if (options.ReformTheGateOfDarkness && !serializer.PutVector(value.BuffInfo, static (s, item) => s.Put(item))) return false;
        if (!serializer.Put(value.Win) || !serializer.Put(value.Lose) || !serializer.PutMap(value.DungeonClear, static (s, key) => s.Put(key), static (s, item) => s.Put(item)) || !serializer.PutMap(value.TCClear, static (s, key) => s.Put(key), static (s, item) => s.Put(item))) return false;
        if (options.LimitedDungeonPlayTimes && !serializer.PutMap(value.DungeonPlay, static (s, key) => s.Put(key), static (s, item) => s.Put(item))) return false;
        if (!serializer.PutMap(value.EquippedItem, static (s, key) => s.Put(key), (s, item) => s.Put(item, options)) || !serializer.Put(value.UnitSkillData, options) || !serializer.Put(value.IsParty) || !serializer.Put(value.SpiritMax) || !serializer.Put(value.Spirit) || !serializer.Put(value.IsGameBang)) return false;
        if (options.PcBangType && !serializer.Put(value.PcBangType)) return false;
        if (options.TitleDataSize) { if (!serializer.Put(value.TitleId)) return false; } else if (!serializer.Put(value.LegacyTitleId)) return false;
        if (options.GuildTest && !serializer.Put(value.UserGuildInfo)) return false;
        return PutTrailingFields(serializer, value, options);
    }
    public static bool Get(this KSerializer serializer, KUnitInfo value, KUnitInfoWireOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value); options ??= KUnitInfoWireOptions.Default;
        if (!GetBase(serializer, value) || !GetPvp(serializer, value, options) || !serializer.Get(out int sPoint) || !serializer.Get(out int csPoint) || !serializer.Get(out int maxCsPoint) || !serializer.GetW(out var csPointEndDate) || !serializer.Get(out int nowBaseLevelExp) || !serializer.Get(out int nextBaseLevelExp) || !serializer.Get(out int straightVictories) || !serializer.Get(value.Stat) || !serializer.Get(value.GameStat) || !GetPosition(serializer, value, options)) return false;
        value.SPoint=sPoint; value.CsPoint=csPoint; value.MaxCsPoint=maxCsPoint; value.CsPointEndDate=csPointEndDate; value.NowBaseLevelExp=nowBaseLevelExp; value.NextBaseLevelExp=nextBaseLevelExp; value.StraightVictories=straightVictories;
        if (options.ReformTheGateOfDarkness && !serializer.GetVector(value.BuffInfo, static s => ReadBuff(s))) return false;
        if (!serializer.Get(out int win) || !serializer.Get(out int lose) || !serializer.GetMap(value.DungeonClear, static s => ReadInt(s), static s => ReadDungeonClear(s)) || !serializer.GetMap(value.TCClear, static s => ReadInt(s), static s => ReadTCClear(s))) return false; value.Win=win; value.Lose=lose;
        if (options.LimitedDungeonPlayTimes && !serializer.GetMap(value.DungeonPlay, static s => ReadInt(s), static s => ReadDungeonPlay(s))) return false;
        if (!serializer.GetMap(value.EquippedItem, static s => ReadInt(s), s => ReadInventoryItem(s, options)) || !serializer.Get(value.UnitSkillData, options) || !serializer.Get(out bool isParty) || !serializer.Get(out int spiritMax) || !serializer.Get(out int spirit) || !serializer.Get(out bool isGameBang)) return false; value.IsParty=isParty; value.SpiritMax=spiritMax; value.Spirit=spirit; value.IsGameBang=isGameBang;
        if (options.PcBangType) { if (!serializer.Get(out int pcBangType)) return false; value.PcBangType=pcBangType; }
        if (options.TitleDataSize) { if (!serializer.Get(out int titleId)) return false; value.TitleId=titleId; } else { if (!serializer.Get(out short legacyTitleId)) return false; value.LegacyTitleId=legacyTitleId; }
        if (options.GuildTest && !serializer.Get(value.UserGuildInfo)) return false; return GetTrailingFields(serializer, value, options);
    }

    private static bool PutBase(KSerializer serializer, KUnitInfo value) => serializer.Put(value.OwnerUserUid) && serializer.Put(value.AuthLevel) && serializer.Put(value.UnitUid) && serializer.Put(value.KnmSerialNumber) && serializer.Put(value.UnitClass) && serializer.PutW(value.NickName) && serializer.PutW(value.Ip) && serializer.Put(value.Port) && serializer.Put(value.Ed) && serializer.Put(value.Level) && serializer.Put(value.Exp);
    private static bool GetBase(KSerializer serializer, KUnitInfo value)
    {
        if (!serializer.Get(out long ownerUserUid) || !serializer.Get(out sbyte authLevel) || !serializer.Get(out long unitUid) || !serializer.Get(out uint knmSerialNumber) || !serializer.Get(out sbyte unitClass) || !serializer.GetW(out var nickName) || !serializer.GetW(out var ip) || !serializer.Get(out ushort port) || !serializer.Get(out int ed) || !serializer.Get(out byte level) || !serializer.Get(out int exp)) return false;
        value.OwnerUserUid=ownerUserUid; value.AuthLevel=authLevel; value.UnitUid=unitUid; value.KnmSerialNumber=knmSerialNumber; value.UnitClass=unitClass; value.NickName=nickName; value.Ip=ip; value.Port=port; value.Ed=ed; value.Level=level; value.Exp=exp; return true;
    }
    private static bool PutPvp(KSerializer serializer, KUnitInfo value, KUnitInfoWireOptions options)
    {
        if (options.PvpNewSystem)
        {
            if (!serializer.Put(value.OfficialMatchCount) || !serializer.Put(value.Rating) || !serializer.Put(value.MaxRating) || !serializer.Put(value.RPoint) || !serializer.Put(value.APoint) || !serializer.Put(value.IsWinBeforeMatch)) return false;
            if (options.PvpSeason2 && (!serializer.Put(value.Rank) || !serializer.Put(value.KFactor) || !serializer.Put(value.IsRedistributionUser) || !serializer.Put(value.PastSeasonWin))) return false; return true;
        }
        return serializer.Put(value.PvpEmblem) && serializer.Put(value.VsPoint) && serializer.Put(value.VsPointMax);
    }
    private static bool GetPvp(KSerializer serializer, KUnitInfo value, KUnitInfoWireOptions options)
    {
        if (options.PvpNewSystem)
        {
            if (!serializer.Get(out int officialMatchCount) || !serializer.Get(out int rating) || !serializer.Get(out int maxRating) || !serializer.Get(out int rPoint) || !serializer.Get(out int aPoint) || !serializer.Get(out bool isWinBeforeMatch)) return false;
            value.OfficialMatchCount=officialMatchCount; value.Rating=rating; value.MaxRating=maxRating; value.RPoint=rPoint; value.APoint=aPoint; value.IsWinBeforeMatch=isWinBeforeMatch;
            if (!options.PvpSeason2) return true;
            if (!serializer.Get(out sbyte rank) || !serializer.Get(out float kFactor) || !serializer.Get(out bool isRedistributionUser) || !serializer.Get(out int pastSeasonWin)) return false;
            value.Rank=rank; value.KFactor=kFactor; value.IsRedistributionUser=isRedistributionUser; value.PastSeasonWin=pastSeasonWin; return true;
        }
        if (!serializer.Get(out int pvpEmblem) || !serializer.Get(out int vsPoint) || !serializer.Get(out int vsPointMax)) return false; value.PvpEmblem=pvpEmblem; value.VsPoint=vsPoint; value.VsPointMax=vsPointMax; return true;
    }
    private static bool PutPosition(KSerializer serializer, KUnitInfo value, KUnitInfoWireOptions options) => options.BattleFieldSystem ? serializer.Put(value.LastPosition) : serializer.Put(value.LegacyMapId) && serializer.Put(value.LegacyLastTouchLineIndex) && serializer.Put(value.LegacyLastPosValue);
    private static bool GetPosition(KSerializer serializer, KUnitInfo value, KUnitInfoWireOptions options)
    {
        if (options.BattleFieldSystem) return serializer.Get(value.LastPosition);
        if (!serializer.Get(out int mapId) || !serializer.Get(out byte lineIndex) || !serializer.Get(out ushort posValue)) return false; value.LegacyMapId=mapId; value.LegacyLastTouchLineIndex=lineIndex; value.LegacyLastPosValue=posValue; return true;
    }
    private static bool PutTrailingFields(KSerializer serializer, KUnitInfo value, KUnitInfoWireOptions options)
    {
        if (options.UnitWaitDelete && (!serializer.PutW(value.LastDate) || !serializer.Put(value.Deleted) || !serializer.Put(value.DeleteAvailableDate) || !serializer.Put(value.RestoreAvailableDate))) return false;
        if (options.AddWarpButton && !serializer.Put(value.WarpVipEndDate)) return false;
        if (options.GrowUpSocket && (!serializer.Put(value.EventQuestClearCount) || !serializer.Put(value.ExchangeCount))) return false;
        if (options.ChinaSpiritEvent) { if (value.ChinaSpirit.Length != 6) return false; foreach (var spirit in value.ChinaSpirit) if (!serializer.Put(spirit)) return false; }
        if (options.RecruitEventQuestForNewUser && !serializer.Put(value.Recruit)) return false;
        if (options.NewYearEvent2014 && (!serializer.Put(value.OldYearMissionRewardedLevel) || !serializer.Put(value.NewYearMissionStepId))) return false; return true;
    }
    private static bool GetTrailingFields(KSerializer serializer, KUnitInfo value, KUnitInfoWireOptions options)
    {
        if (options.UnitWaitDelete) { if (!serializer.GetW(out var lastDate) || !serializer.Get(out bool deleted) || !serializer.Get(out long deleteAvailableDate) || !serializer.Get(out long restoreAvailableDate)) return false; value.LastDate=lastDate; value.Deleted=deleted; value.DeleteAvailableDate=deleteAvailableDate; value.RestoreAvailableDate=restoreAvailableDate; }
        if (options.AddWarpButton) { if (!serializer.Get(out long warpVipEndDate)) return false; value.WarpVipEndDate=warpVipEndDate; }
        if (options.GrowUpSocket) { if (!serializer.Get(out int eventQuestClearCount) || !serializer.Get(out int exchangeCount)) return false; value.EventQuestClearCount=eventQuestClearCount; value.ExchangeCount=exchangeCount; }
        if (options.ChinaSpiritEvent) { if (value.ChinaSpirit.Length != 6) return false; for (var index=0; index<value.ChinaSpirit.Length; index++) { if (!serializer.Get(out int spirit)) return false; value.ChinaSpirit[index]=spirit; } }
        if (options.RecruitEventQuestForNewUser) { if (!serializer.Get(out bool recruit)) return false; value.Recruit=recruit; }
        if (options.NewYearEvent2014) { if (!serializer.Get(out byte oldYearMissionRewardedLevel) || !serializer.Get(out int newYearMissionStepId)) return false; value.OldYearMissionRewardedLevel=oldYearMissionRewardedLevel; value.NewYearMissionStepId=newYearMissionStepId; }
        return true;
    }

    private static bool HasExpectedSkillSlots(KUnitSkillData value) => value.EquippedSkill.Length == KUnitSkillData.EquippedSkillSlotCount && value.EquippedSkillSlotB.Length == KUnitSkillData.EquippedSkillSlotCount;
    private static bool PutSocketVector(KSerializer serializer, IReadOnlyList<int> values, bool useInt32) => useInt32 ? serializer.PutVector(values, static (s, value) => s.Put(value)) : serializer.PutVector(values, static (s, value) => s.Put(checked((short)value)));
    private static bool GetSocketVector(KSerializer serializer, ICollection<int> values, bool useInt32) => useInt32 ? serializer.GetVector(values, static s => ReadInt(s)) : serializer.GetVector(values, static s => ReadShortAsIntPair(s));
    private static bool PutSlotId(KSerializer serializer, int value, bool expanded) => expanded ? serializer.Put(checked((short)value)) : serializer.Put(checked((sbyte)value));
    private static bool GetSlotId(KSerializer serializer, out int value, bool expanded) => expanded ? ReadShortAsInt(serializer, out value) : ReadSByteAsInt(serializer, out value);
    private static (bool Ok, int Value) ReadInt(KSerializer serializer) { var ok=serializer.Get(out int value); return (ok,value); }
    private static (bool Ok, short Value) ReadShort(KSerializer serializer) { var ok=serializer.Get(out short value); return (ok,value); }
    private static (bool Ok, int Value) ReadShortAsIntPair(KSerializer serializer) { var ok=serializer.Get(out short value); return (ok,value); }
    private static (bool Ok, float Value) ReadFloat(KSerializer serializer) { var ok=serializer.Get(out float value); return (ok,value); }
    private static bool ReadShortAsInt(KSerializer serializer, out int value) { var ok=serializer.Get(out short raw); value=raw; return ok; }
    private static bool ReadSByteAsInt(KSerializer serializer, out int value) { var ok=serializer.Get(out sbyte raw); value=raw; return ok; }
    private static (bool Ok, KSkillData Value) ReadSkill(KSerializer serializer) { var value=new KSkillData(); return (serializer.Get(value),value); }
    private static (bool Ok, KBuffBehaviorFactor Value) ReadBehaviorFactor(KSerializer serializer) { var value=new KBuffBehaviorFactor(); return (serializer.Get(value),value); }
    private static (bool Ok, KBuffFinalizerFactor Value) ReadFinalizerFactor(KSerializer serializer) { var value=new KBuffFinalizerFactor(); return (serializer.Get(value),value); }
    private static (bool Ok, KBuffInfo Value) ReadBuff(KSerializer serializer) { var value=new KBuffInfo(); return (serializer.Get(value),value); }
    private static (bool Ok, KDungeonClearInfo Value) ReadDungeonClear(KSerializer serializer) { var value=new KDungeonClearInfo(); return (serializer.Get(value),value); }
    private static (bool Ok, KTCClearInfo Value) ReadTCClear(KSerializer serializer) { var value=new KTCClearInfo(); return (serializer.Get(value),value); }
    private static (bool Ok, KDungeonPlayInfo Value) ReadDungeonPlay(KSerializer serializer) { var value=new KDungeonPlayInfo(); return (serializer.Get(value),value); }
    private static (bool Ok, KInventoryItemInfo Value) ReadInventoryItem(KSerializer serializer, KUnitInfoWireOptions options) { var value=new KInventoryItemInfo(); return (serializer.Get(value,options),value); }
}
