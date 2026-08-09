namespace KncWX2Server.Runtime.Center;

public sealed record RoomSlotInfo(
    int Index,
    RoomSlotState SlotState,
    int TeamNum,
    bool Host,
    bool Ready,
    bool PitIn,
    bool Trade,
    long UnitUid);

public sealed class RoomSlot
{
    public RoomSlot(int slotId) => InitSlot(slotId);

    public int SlotId { get; private set; }
    public int Team { get; private set; }
    public RoomSlotStateMachine StateMachine { get; } = new();
    public RoomUser? User { get; private set; }

    public bool IsOpened =>
        StateMachine.State is RoomSlotState.Init or RoomSlotState.Assigned;

    public bool IsOccupied =>
        StateMachine.State is RoomSlotState.Assigned && User is not null;

    public void AssignTeam(int gameMode)
    {
        Team = gameMode switch
        {
            0 or 1 => SlotId / 4 == 0 ? 0 : 1,
            2 => SlotId,
            _ => 0,
        };
    }

    public long GetCID() => IsOccupied ? User!.Cid : 0;

    public bool Enter(RoomUser user)
    {
        if (!IsOpened || IsOccupied || user is null)
        {
            return false;
        }

        User = user;
        user.SetSlotId(SlotId);
        user.SetTeam(Team);
        return StateMachine.Send(RoomSlotInput.ToAssigned);
    }

    public bool Leave()
    {
        if (!IsOccupied)
        {
            return true;
        }

        StateMachine.Send(RoomSlotInput.ToInit);
        User = null;
        return true;
    }

    public bool Open()
    {
        if (IsOpened || IsOccupied)
        {
            return true;
        }

        return StateMachine.Send(RoomSlotInput.ToInit);
    }

    public bool Close()
    {
        if (!IsOpened)
        {
            return true;
        }

        if (IsOccupied)
        {
            return false;
        }

        return StateMachine.Send(RoomSlotInput.ToClosed);
    }

    public bool ToggleOpenClose() => StateMachine.State switch
    {
        RoomSlotState.Init => StateMachine.Send(RoomSlotInput.ToClosed),
        RoomSlotState.Closed => StateMachine.Send(RoomSlotInput.ToInit),
        RoomSlotState.Assigned => false,
        _ => false,
    };

    public void ResetSlot()
    {
        if (StateMachine.State is RoomSlotState.Closed or RoomSlotState.Assigned)
        {
            StateMachine.Send(RoomSlotInput.ToInit);
        }

        User = null;
    }

    public RoomSlotInfo GetRoomSlotInfo()
    {
        var state = StateMachine.State;

        if (IsOccupied)
        {
            var user = User!;
            var calculatedState = (int)state +
                                  (int)user.StateMachine.State -
                                  (int)RoomUserState.Init;

            if (calculatedState > (int)RoomSlotState.Assigned)
            {
                calculatedState = (int)RoomSlotState.Assigned;
            }

            state = (RoomSlotState)calculatedState;

            return new(
                SlotId,
                state,
                Team,
                user.IsHost,
                user.IsReady,
                user.IsPitIn,
                user.IsInTrade,
                user.UnitUid);
        }

        return new(
            SlotId,
            state,
            Team,
            false,
            false,
            false,
            false,
            0);
    }

    private void InitSlot(int id)
    {
        SlotId = id;
        AssignTeam(0);
        User = null;
        StateMachine.Force(RoomSlotState.Init);
    }
}