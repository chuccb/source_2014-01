namespace KncWX2Server.Runtime.Center;

/// <summary>Named room events. Numeric wire IDs must come from the generated protocol table.</summary>
public enum RoomEventId : ushort
{
    ServerOnReq,ServerOnAck,ServerOffNot,NewUserJoinReq,NewUserJoinAck,
    UpdateServerInfoReq,UpdateServerInfoAck,OpenPvpRoomReq,OpenDungeonRoomReq,OpenRoomAck,
    JoinRoomReq,JoinRoomAck,JoinRoomNot,LeaveRoomReq,LeaveRoomAck,LeaveRoomNot,
    IntrudeGameReq,IntrudeGameAck,IntrudeGameNot,BanUserReq,BanUserAck,BanUserNot,
    RoomListInfoNot,RoomOptionInfoNot,RoomSlotInfoNot,ChangeRoomOptionInfoReq,ChangeRoomOptionInfoAck,
    ChangeTeamReq,ChangeTeamAck,ChangeTeamNot,ChangeReadyReq,ChangeReadyAck,ChangeReadyNot,
    ChangePitInReq,ChangePitInAck,ChangePitInNot,ChangeSlotOpenReq,ChangeSlotOpenAck,ChangeSlotOpenNot,
    ChangeRoomSlotInfoReq,ChangeRoomSlotInfoAck,GameStartReq,GameStartAck,GameStartNot,
    GameLoadingReq,GameLoadingAck,GameLoadingNot,GameLoadingAllUnitOkNot,
    ChatReq,ChatAck,ChatNot,PlayStartNot,RemainingPlayTimeNot,PlayTimeOutNot,
    EndGameReq,EndGameAck,EndGameNot,StateChangeResultReq,StateChangeResultAck,StateChangeResultNot
}
