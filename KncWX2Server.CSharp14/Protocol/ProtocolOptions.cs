namespace KncWX2Server.CSharp14.Protocol;

/// <summary>Build-time protocol feature switches corresponding to native SERV_* wire-layout macros.</summary>
public sealed record ProtocolOptions
{
    public static ProtocolOptions Default { get; } = new();
    public bool RelationshipSystem { get; init; }
    public bool DbConnectionSecurity { get; init; }
    public bool ToonilandChanneling { get; init; }
    public bool ChannelingUserManager { get; init; }
    public bool BubbleFighterTogetherEvent { get; init; }
    public bool ChangeNexonAuthAtlLevel { get; init; }
    public bool OtpAuth { get; init; }
    public bool CashItemList { get; init; }
    public bool SecondSecurity { get; init; }
    public bool DllListCheckBeforeLoading { get; init; }
    public bool FixedDateEvent { get; init; }
    public bool HackingUserCheckCount { get; init; }
    public bool MachineIdDuplicateCheck { get; init; }
    public bool SerialNumberAvailabilityCheckInGameServer { get; init; }
    public bool CheckMachineLocalTime { get; init; }
    public bool CogOtpVerify { get; init; }
    public bool CountryTh { get; init; }
    public bool Steam { get; init; }
    public bool ChannelingAeria { get; init; }
    public bool PetSystem { get; init; }
    public bool PetIdDataTypeChange { get; init; }
    public bool PetAutoLooting { get; init; }
    public bool FreeAutoLooting { get; init; }
    public bool PeriodPet { get; init; }
    public bool ReformQuest { get; init; }
    public bool ItemOptionDataSize { get; init; }
    public bool ExpandSlotIdDataSize { get; init; }
    public bool NewItemSystem201305 { get; init; }
    public bool GoldTicket { get; init; }
    public bool LimitedDungeonPlayTimes { get; init; }
    public bool GuildSkillTest { get; init; }
    public bool SkillNote { get; init; }
    public bool DeleteItem { get; init; }
    public bool BattleFieldSystem { get; init; }
    public bool RidingPetSystm { get; init; }
    public bool ReformTheGateOfDarkness { get; init; }
    public bool CoexistenceFestivalRoomBuff { get; init; }
    public bool DungeonItem { get; init; }
    public bool PvpRematch { get; init; }
    public bool NewDefenceDungeon { get; init; }
    public bool ServerBuffSystem { get; init; }
    public bool PvpNewSystem { get; init; }
    public bool PvpSeason2 { get; init; }
    public bool DeleteRoomUserInfoData { get; init; }
    public bool AddDungeonLogColumnNum2 { get; init; }
    public bool PaymentItemWithConsumingOtherItem { get; init; }
    public bool DungeonClearPaymentItem { get; init; }
    public bool DungeonClearPaymentItemFix { get; init; }
    public bool PaymentItemOnGoingQuest { get; init; }
    public bool ComeBackUserReward { get; init; }
    public bool NewHenirTest { get; init; }
    public bool VisitCashShop { get; init; }
    public bool GateOfDarknessSupportEvent { get; init; }
    public bool RelationshipEventInt { get; init; }
    public bool RecruitEventBase { get; init; }
    public bool PcBangType { get; init; }
    public bool TitleDataSize { get; init; }
    public bool GuildTest { get; init; }
    public bool UnitWaitDelete { get; init; }
    public bool AddWarpButton { get; init; }
    public bool GrowUpSocket { get; init; }
    public bool ChinaSpiritEvent { get; init; }
    public bool RecruitEventQuestForNewUser { get; init; }
    public bool NewYearEvent2014 { get; init; }
    public bool ServerGroupEventSystem { get; init; }
    public bool ServerIntegration { get; init; }
    public bool FromChannelToLoginProxy { get; init; }
    public bool PvpBossCombatTest { get; init; }
}
