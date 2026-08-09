namespace KncWX2Server.Runtime.Center;

public sealed class RoomUser
{
    public const int DefaultObserverSlotCount=3;
    public const double LoadingTimeoutSeconds=60.0;
    public const int SecretStageNone=0;
    public const int SecretStageAgree=1;
    public long GSUid { get; set; }
    public long UserUid { get; set; }
    public long UnitUid { get; set; }
    public long Cid { get=>UnitUid; set=>UnitUid=value; }
    public long PartyUid { get; set; }
    public int Level { get; private set; }=1;
    public int UnitType { get; private set; }
    public int Team { get; private set; }
    public int SlotId { get; private set; }=-1;
    public bool IsHost { get; private set; }
    public bool IsReady { get; private set; }
    public bool IsPitIn { get; private set; }
    public bool IsInTrade { get; private set; }
    public bool IsPvpNpc { get; private set; }
    public bool IsBoss { get; private set; }
    public bool IsEnterCashShopUser { get; private set; }
    public bool IsSuccessResult { get; private set; }=true;
    public bool IsGameBang { get; private set; }
    public bool HavePet { get; private set; }
    public int AgreeEnterSecretStage { get; private set; }
    public bool IsIntrude { get; private set; }
    public bool IsRingOfPvpRebirth { get; private set; }
    public int RewardEXP { get; private set; }
    public int RewardPartyEXP { get; private set; }
    public int LoadingProgress { get; private set; }=-1;
    public bool IsStageLoaded { get; private set; }
    public int NumKill { get; private set; }
    public int NumMDKill { get; private set; }
    public int NumDie { get; private set; }
    public bool IsDie { get; private set; }
    public float HP { get; private set; }=-1f;
    public int StageId { get; private set; }=-1;
    public int SubStageId { get; private set; }=-1;
    public int RebirthPos { get; private set; }
    public bool IsAcceptRematch { get; private set; }
    public bool IsPrepareForDefence { get; private set; }
    public bool IsRecvEnterPopupReply { get; private set; }
    public bool IsEnterDefenceDungeon { get; private set; }
    public int MatchWaitTime { get; private set; }
    public int AutoPartyWaitTime { get; private set; }
    public int PvpNpcId { get; private set; }
    public long RidingPetUid { get; private set; }
    public ushort RidingPetId { get; private set; }
    public RoomUserStateMachine StateMachine { get; }=new();
    public bool IsPlaying=>StateMachine.State is RoomUserState.Load or RoomUserState.Play;
    public void SetLevel(int level)=>Level=level;
    public void SetUnitType(int unitType)=>UnitType=unitType;
    public void SetTeam(int team)=>Team=team;
    public void SetSlotId(int slotId)=>SlotId=slotId;
    public bool SetHost(bool host){IsHost=host;return true;}
    public bool SetReady(bool ready){IsReady=ready;return true;}
    public void SetPitIn(bool value)=>IsPitIn=value;
    public void SetTrade(bool value){IsInTrade=value;if(value)IsReady=false;}
    public void SetPvpNpc(bool value)=>IsPvpNpc=value;
    public void SetPvpNpcId(int value)=>PvpNpcId=value;
    public void SetIsBoss(bool value)=>IsBoss=value;
    public void SetEnterCashShopUser(bool value)=>IsEnterCashShopUser=value;
    public void SetSuccessResult(bool value)=>IsSuccessResult=value;
    public void SetGameBang(bool value)=>IsGameBang=value;
    public void SetHavePet(bool value)=>HavePet=value;
    public void SetAgreeEnterSecretStage(int value)=>AgreeEnterSecretStage=value;
    public bool SetIsIntrude(bool value){IsIntrude=value;return true;}
    public void SetRingOfPvpRebirth(bool value)=>IsRingOfPvpRebirth=value;
    public void SetRewardEXP(int value)=>RewardEXP=value;
    public void SetRewardPartyEXP(int value)=>RewardPartyEXP=value;
    public void SetLoadingProgress(int value)=>LoadingProgress=value;
    public void SetStageLoaded(bool value)=>IsStageLoaded=value;
    public void IncreaseKill()=>NumKill++;
    public void IncreaseMDKill()=>NumMDKill++;
    public void IncreaseDie()=>NumDie++;
    public void SetDie(bool value)=>IsDie=value;
    public void SetHP(float value)=>HP=value;
    public void SetStage(int value)=>StageId=value;
    public void SetSubStage(int value)=>SubStageId=value;
    public void SetRebirthPos(int value)=>RebirthPos=value;
    public void SetRematch(bool value)=>IsAcceptRematch=value;
    public void SetPrepareForDefence(bool value)=>IsPrepareForDefence=value;
    public void SetRecvEnterPopupReply(bool value)=>IsRecvEnterPopupReply=value;
    public void SetEnterDefenceDungeon(bool value)=>IsEnterDefenceDungeon=value;
    public void SetMatchWaitTime(int value)=>MatchWaitTime=value;
    public void SetAutoPartyWaitTime(int value)=>AutoPartyWaitTime=value;
    public void SetRidingPetInfo(long petUid, ushort petId){RidingPetUid=petUid;RidingPetId=petId;}
    public void StartGame(){LoadingProgress=0;IsStageLoaded=false;StageId=-1;SubStageId=-1;RebirthPos=0;NumKill=0;NumMDKill=0;NumDie=0;IsDie=false;HP=-1f;IsSuccessResult=true;RewardEXP=0;RewardPartyEXP=0;StateMachine.Send(RoomUserInput.ToLoad);}
    public void StartPlay(){LoadingProgress=-1;IsStageLoaded=false;NumKill=0;NumMDKill=0;NumDie=0;IsDie=false;HP=-1f;IsSuccessResult=true;StateMachine.Send(RoomUserInput.ToPlay);}
    public void EndPlay(){LoadingProgress=0;IsStageLoaded=false;StageId=-1;SubStageId=-1;RebirthPos=0;StateMachine.Send(RoomUserInput.ToResult);}
    public void EndGame(){SetReady(false);LoadingProgress=-1;IsStageLoaded=false;IsSuccessResult=true;StateMachine.Send(RoomUserInput.ToInit);}
    public int GetPercentHP(int baseHp)=>baseHp<=0?0:(int)(HP*100f/baseHp);
}