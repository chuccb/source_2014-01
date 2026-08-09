namespace KncWX2Server.Runtime.Center;

public enum RoomState { Init=0, Close=1, Wait=2, TimeCount=3, Load=4, Play=5, Result=6, ReturnToField=7, WaitForDefence=8 }
public enum RoomInput { ToInit=0, ToClose=1, ToWait=2, ToTimeCount=3, ToLoad=4, ToPlay=5, ToResult=6, ToReturnToField=7, ToWaitForDefence=8 }

public sealed class RoomStateMachine
{
    public RoomState State { get; private set; }=RoomState.Init;
    public event Action<RoomState,RoomState>? Transitioned;
    public bool Send(RoomInput input)
    {
        var next=(State,input) switch
        {
            (RoomState.Init,RoomInput.ToWait)=>RoomState.Wait,
            (RoomState.Close,RoomInput.ToInit)=>RoomState.Init,
            (RoomState.Wait,RoomInput.ToClose)=>RoomState.Close,
            (RoomState.Wait,RoomInput.ToLoad)=>RoomState.Load,
            (RoomState.Wait,RoomInput.ToTimeCount)=>RoomState.TimeCount,
            (RoomState.Wait,RoomInput.ToReturnToField)=>RoomState.ReturnToField,
            (RoomState.Wait,RoomInput.ToWaitForDefence)=>RoomState.WaitForDefence,
            (RoomState.TimeCount,RoomInput.ToClose)=>RoomState.Close,
            (RoomState.TimeCount,RoomInput.ToLoad)=>RoomState.Load,
            (RoomState.Load,RoomInput.ToClose)=>RoomState.Close,
            (RoomState.Load,RoomInput.ToPlay)=>RoomState.Play,
            (RoomState.Play,RoomInput.ToClose)=>RoomState.Close,
            (RoomState.Play,RoomInput.ToResult)=>RoomState.Result,
            (RoomState.Result,RoomInput.ToClose)=>RoomState.Close,
            (RoomState.Result,RoomInput.ToWait)=>RoomState.Wait,
            (RoomState.ReturnToField,RoomInput.ToClose)=>RoomState.Close,
            (RoomState.ReturnToField,RoomInput.ToWait)=>RoomState.Wait,
            (RoomState.WaitForDefence,RoomInput.ToClose)=>RoomState.Close,
            (RoomState.WaitForDefence,RoomInput.ToWait)=>RoomState.Wait,
            _=>(RoomState?)null
        };
        if(next is null)return false;
        var previous=State;State=next.Value;Transitioned?.Invoke(previous,State);return true;
    }
    public void Force(RoomState state){var previous=State;State=state;if(previous!=state)Transitioned?.Invoke(previous,state);}
}
