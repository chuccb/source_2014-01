namespace KncWX2Server.Runtime.Center;

public sealed class RoomUser
{
    public const int DefaultObserverSlotCount=3;
    public long GSUid { get; }
    public long UserUid { get; }
    public long Cid { get; }
    public long PartyUid { get; }
    public int Team { get; private set; }
    public int SlotId { get; private set; }=-1;
    public bool IsHost { get; private set; }
    public bool IsReady=> (IsHost||_ready) && !IsInTrade;
    public bool IsPitIn { get; private set; }
    public bool IsInTrade { get; private set; }
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
    public RoomUserState State { get; private set; }=RoomUserState.Init;
    private bool _ready;

    public RoomUser(long gsUid,long userUid,long cid,long partyUid=0){GSUid=gsUid;UserUid=userUid;Cid=cid;PartyUid=partyUid;}
    public void SetTeam(int value)=>Team=value;
    public void SetSlotId(int value)=>SlotId=value;
    public bool SetHost(bool value){if(value&&!SetReady(true))return false;IsHost=value;if(!value)_ready=false;return true;}
    public bool SetReady(bool value){if(value&&IsInTrade)return false;_ready=value;return true;}
    public void SetPitIn(bool value)=>IsPitIn=value;
    public void SetTrade(bool value){if(value)_ready=false;IsInTrade=value;}
    public void SetLoadingProgress(int value)=>LoadingProgress=value;
    public void SetStageLoaded(bool value)=>IsStageLoaded=value;
    public void SetDie(bool value)=>IsDie=value;
    public void SetHP(float value)=>HP=value;
    public void SetStage(int value)=>StageId=value;
    public void SetSubStage(int value)=>SubStageId=value;
    public void SetRebirthPos(int value)=>RebirthPos=value;
    public void IncreaseKill()=>NumKill++;
    public void IncreaseMDKill()=>NumMDKill++;
    public void IncreaseDie()=>NumDie++;
    public bool StartGame()=>State==RoomUserState.Init&&(State=RoomUserState.Load)==RoomUserState.Load;
    public bool StartPlay()=>State==RoomUserState.Load&&(State=RoomUserState.Play)==RoomUserState.Play;
    public bool StartResult()=>State==RoomUserState.Play&&(State=RoomUserState.Result)==RoomUserState.Result;
    public bool EndResult()=>State==RoomUserState.Result&&(State=RoomUserState.Init)==RoomUserState.Init;
    public bool EndGame()=>State switch{RoomUserState.Load or RoomUserState.Play or RoomUserState.Result=>(State=RoomUserState.Init)==RoomUserState.Init,_=>true};
    public bool IsPlaying()=>State==RoomUserState.Play;
}
