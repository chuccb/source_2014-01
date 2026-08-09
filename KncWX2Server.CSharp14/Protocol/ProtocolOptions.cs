namespace KncWX2Server.CSharp14.Protocol;

/// <summary>
/// Build-time protocol feature switches corresponding to native SERV_* macros.
/// Only options that change serialized wire layout belong here.
/// </summary>
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

    public bool MachineIdDuplicateCheck { get; init; }
    public bool SerialNumberAvailabilityCheckInGameServer { get; init; }
    public bool CheckMachineLocalTime { get; init; }
    public bool CogOtpVerify { get; init; }
    public bool CountryTh { get; init; }
    public bool Steam { get; init; }
    public bool ChannelingAeria { get; init; }

    public bool PetSystem { get; init; }
    public bool ItemOptionDataSize { get; init; }
    public bool ExpandSlotIdDataSize { get; init; }
    public bool NewItemSystem201305 { get; init; }
    public bool GoldTicket { get; init; }
    public bool LimitedDungeonPlayTimes { get; init; }
}
