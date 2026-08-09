namespace KncWX2Server.Runtime.Center;

public sealed class RoomUser
{
    public const double LoadingTimeoutSeconds=60.0;
    public long GSUid { get; set; }
    public long UserUid { get; set; }
    public long UnitUid { get; set; }
    public long Cid { get=>UnitUid; set=>UnitUid=value; }
    public long PartyUid { get; set; }
    public int Team { get; private set; }
    public int SlotId { get; private set; }=-1;
    public bool IsHost { get; private set; }
    public bool IsReady => (IsHost||_ready)&&!IsInTrade;
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
    public RoomUserStateMachine StateMachine { get; }=new();
    private bool _ready;
    public void SetTeam(int team)=>Team=team;
    public void SetSlotId(int slotId)=>SlotId=slotId;
    public bool SetHost(bool host){SetReady(host);IsHost=host;return true;}
    public bool SetReady(bool ready){if(ready&&IsInTrade)return false;_ready=ready;return true;}
    public void SetPitIn(bool value)=>IsPitIn=value;
    public void SetTrade(bool value){if(value)SetReady(false);IsInTrade=value;}
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
    public int GetPercentHP(int baseHp)=>baseHp<=0?0:(int)(HP*100f/baseHp);
}
