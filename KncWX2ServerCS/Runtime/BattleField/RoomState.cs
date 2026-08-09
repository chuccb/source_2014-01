namespace KncWX2Server.Runtime.BattleField;

public enum RoomType { Invalid=-1, Pvp=0, Dungeon, Square, Trade, TrainingCenter, PersonalShop, Arcade, BattleField }
public enum RoomState { Init=1, Closed, Wait, TimeCount, Loading, Play, Result, ReturnToField }
public enum RoomChatType { All=0, Team, Whisper }
public enum TeamNum { None=-1, Red=0, Blue=1, Monster=2 }
public enum SlotState { Empty=1, Close, Wait, Loading, Play }
public enum DungeonGetItemType { None=0, Random, Person, End }

public sealed record RoomSimpleInfo(long RoomUid,string RoomName,RoomState State,bool IsPublic,int MaxSlot,int JoinSlot);

public sealed class RoomSlot
{
    public int Index { get; }
    public SlotState State { get; private set; } = SlotState.Close;
    public TeamNum Team { get; set; } = TeamNum.Red;
    public long UnitUid { get; private set; }
    public uint PingTime { get; private set; } = 99999;
    public float ElapsedTimeAfterJoinRoom { get; private set; }
    public bool IsHost { get; set; }
    public bool IsMySlot { get; set; }
    public bool IsReady { get; set; }
    public bool IsPitIn { get; set; }
    public bool IsTrade { get; set; }
    public bool IsObserver { get; set; }
    public RoomSlot(int index)=>Index=index;
    public void SetState(SlotState state)=>State=state;
    public void SetUnit(long uid)=>UnitUid=uid;
    public void SetPing(uint ping)=>PingTime=ping;
    public void Tick(float elapsed){if(UnitUid!=0)ElapsedTimeAfterJoinRoom+=elapsed;}
    public void Clear(){State=SlotState.Close;UnitUid=0;PingTime=99999;ElapsedTimeAfterJoinRoom=0;IsHost=false;IsMySlot=false;IsReady=false;IsPitIn=false;IsTrade=false;IsObserver=false;Team=TeamNum.Red;}
}

public class Room
{
    private readonly RoomSlot[] _slots;
    public long RoomUid { get; }
    public string Name { get; private set; }
    public RoomType Type { get; }
    public RoomState State { get; private set; } = RoomState.Init;
    public bool IsPublic { get; private set; } = true;
    public DungeonGetItemType GetItemType { get; private set; } = DungeonGetItemType.Random;
    public IReadOnlyList<RoomSlot> Slots=>_slots;
    public Room(long roomUid,string name,RoomType type,int maxSlots){if(maxSlots<0)throw new ArgumentOutOfRangeException(nameof(maxSlots));RoomUid=roomUid;Name=name;Type=type;_slots=Enumerable.Range(0,maxSlots).Select(i=>new RoomSlot(i)).ToArray();}
    public void SetState(RoomState state)=>State=state;
    public void SetPublic(bool value)=>IsPublic=value;
    public void SetName(string name)=>Name=name;
    public void SetGetItemType(DungeonGetItemType value)=>GetItemType=value;
    public int JoinSlotCount=>_slots.Count(x=>x.UnitUid!=0);
    public int MaxSlot=>_slots.Length;
    public RoomSimpleInfo GetSimpleInfo()=>new(RoomUid,Name,State,IsPublic,MaxSlot,JoinSlotCount);
    public RoomSlot? FindSlot(long uid)=>_slots.FirstOrDefault(x=>x.UnitUid==uid);
    public RoomSlot? FindEmptySlot()=>_slots.FirstOrDefault(x=>x.State==SlotState.Empty||x.State==SlotState.Close);
    public bool AddUnit(long uid,out int slotIndex){var slot=FindEmptySlot();if(slot is null){slotIndex=-1;return false;}slot.SetUnit(uid);slot.SetState(SlotState.Wait);slotIndex=slot.Index;return true;}
    public bool RemoveUnit(long uid){var slot=FindSlot(uid);if(slot is null)return false;slot.Clear();return true;}
    public void Tick(float elapsed){foreach(var slot in _slots)slot.Tick(elapsed);}
}

public sealed class BattleFieldRoom : Room
{
    public uint BattleFieldId { get; private set; }
    public BattleFieldRoom(long roomUid,string name,int maxSlots,uint battleFieldId=uint.MaxValue):base(roomUid,name,RoomType.BattleField,maxSlots)=>BattleFieldId=battleFieldId;
    public void SetBattleFieldId(uint id)=>BattleFieldId=id;
}
