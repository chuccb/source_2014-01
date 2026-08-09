using System;
using System.Collections.Generic;
using System.Linq;

namespace KncWX2Server.Runtime.BattleField;

/// <summary>
/// Runtime representation of CX2BattleFieldRoom.
/// Packet-specific behavior can be layered on top without changing the room lifecycle model.
/// </summary>
public sealed class BattleFieldRoom
{
    private readonly Dictionary<long, int> _unitSlots = [];
    private readonly RoomStateMachine _stateMachine = new();

    public long RoomUid { get; }
    public string Name { get; }
    public int MaxSlot { get; }
    public uint BattleFieldId { get; }
    public bool IsPublic { get; }
    public DungeonGetItemType GetItemType { get; set; } = DungeonGetItemType.Random;

    public RoomState State => _stateMachine.State;
    public int JoinSlotCount => _unitSlots.Count;
    public int FreeSlotCount => MaxSlot - JoinSlotCount;

    public event Action<RoomState, RoomState>? StateChanged;

    public BattleFieldRoom(
        long roomUid,
        string name,
        int maxSlots,
        uint battleFieldId = uint.MaxValue,
        bool isPublic = true)
    {
        if (roomUid <= 0)
            throw new ArgumentOutOfRangeException(nameof(roomUid));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (maxSlots <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSlots));

        RoomUid = roomUid;
        Name = name;
        MaxSlot = maxSlots;
        BattleFieldId = battleFieldId;
        IsPublic = isPublic;

        _stateMachine.Transitioned += HandleStateTransition;
    }

    public bool TryTransition(RoomState next) => _stateMachine.TryTransition(next);

    public void ForceState(RoomState state) => _stateMachine.Force(state);

    public bool AddUnit(long unitUid, out int slot)
    {
        if (unitUid <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitUid));

        if (_unitSlots.TryGetValue(unitUid, out slot))
            return true;

        if (_unitSlots.Count >= MaxSlot)
        {
            slot = -1;
            return false;
        }

        for (var candidate = 0; candidate < MaxSlot; candidate++)
        {
            if (_unitSlots.Values.Contains(candidate))
                continue;

            _unitSlots.Add(unitUid, candidate);
            slot = candidate;
            return true;
        }

        slot = -1;
        return false;
    }

    public bool RemoveUnit(long unitUid) => _unitSlots.Remove(unitUid);

    public bool ContainsUnit(long unitUid) => _unitSlots.ContainsKey(unitUid);

    public bool TryGetUnitSlot(long unitUid, out int slot) => _unitSlots.TryGetValue(unitUid, out slot);

    public RoomSimpleInfo GetSimpleInfo() => new(
        RoomUid,
        Name,
        State,
        IsPublic,
        MaxSlot,
        JoinSlotCount);

    public void Tick(float elapsedSeconds)
    {
        if (elapsedSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));

        // Timer and packet-processing behavior will be ported with their owning systems.
    }

    private void HandleStateTransition(RoomState previous, RoomState next) =>
        StateChanged?.Invoke(previous, next);
}
