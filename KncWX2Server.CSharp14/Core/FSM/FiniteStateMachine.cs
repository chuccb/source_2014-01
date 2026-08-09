using System.Collections.Concurrent;

namespace KncWX2Server.Core.FSM;

/// <summary>
/// Generic Finite State Machine implementation for C# 14.
/// Replaces the C++ FSMclass with async/await support.
/// </summary>
public class FiniteStateMachine<TStateId, TInputId>
    where TStateId : notnull
    where TInputId : notnull
{
    private readonly ConcurrentDictionary<TStateId, IFsmState> _states;
    private readonly ConcurrentDictionary<(TStateId, TInputId), IFsmTransition> _transitions;
    private IFsmState? _currentState;
    private readonly object _stateLock = new();

    public IFsmState? CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    public FiniteStateMachine()
    {
        _states = new();
        _transitions = new();
    }

    /// <summary>
    /// Adds a state to the FSM.
    /// </summary>
    public void AddState(TStateId stateId, IFsmState state)
    {
        _states.TryAdd(stateId, state);
    }

    /// <summary>
    /// Adds a transition to the FSM.
    /// </summary>
    public void AddTransition(TStateId fromState, TInputId input, TStateId toState, IFsmTransition transition)
    {
        _transitions.TryAdd((fromState, input), transition);
    }

    /// <summary>
    /// Sets the initial state and enters it.
    /// </summary>
    public async Task InitializeAsync(TStateId initialStateId, object? context = null)
    {
        if (_states.TryGetValue(initialStateId, out var state))
        {
            lock (_stateLock)
            {
                _currentState = state;
            }
            await state.OnEnterAsync(context);
        }
        else
        {
            throw new InvalidOperationException($"State {initialStateId} not found in FSM.");
        }
    }

    /// <summary>
    /// Processes an input and transitions to the next state if applicable.
    /// </summary>
    public async Task<bool> ProcessInputAsync(TInputId inputId, object? context = null)
    {
        lock (_stateLock)
        {
            if (_currentState == null)
            {
                return false;
            }

            var stateId = _currentState.StateId;
            if (!_transitions.TryGetValue(((dynamic)stateId, inputId), out var transition))
            {
                return false;
            }

            var toStateId = transition.ToStateId;
            if (!_states.TryGetValue((dynamic)toStateId, out var nextState))
            {
                return false;
            }
        }

        return await ExecuteTransitionAsync(inputId, context);
    }

    /// <summary>
    /// Updates the current state.
    /// </summary>
    public async Task UpdateAsync(float deltaTime)
    {
        IFsmState? state;
        lock (_stateLock)
        {
            state = _currentState;
        }

        if (state != null)
        {
            await state.OnUpdateAsync(deltaTime);
        }
    }

    private async Task<bool> ExecuteTransitionAsync(TInputId inputId, object? context)
    {
        IFsmState? currentState;
        IFsmState? nextState = null;
        IFsmTransition? transition = null;

        lock (_stateLock)
        {
            currentState = _currentState;
            if (currentState == null)
            {
                return false;
            }

            var stateId = currentState.StateId;
            if (_transitions.TryGetValue(((dynamic)stateId, inputId), out transition) &&
                _states.TryGetValue((dynamic)transition.ToStateId, out nextState))
            {
                if (!transition is null)
                {
                    _currentState = nextState;
                }
            }
        }

        if (transition != null && await transition.CanTransitionAsync(context))
        {
            await currentState!.OnExitAsync(context);
            await nextState!.OnEnterAsync(context);
            return true;
        }

        return false;
    }
}
