namespace KncWX2Server.Core.FSM;

/// <summary>
/// Represents a state in the Finite State Machine.
/// </summary>
public interface IFsmState
{
    /// <summary>
    /// Gets the state identifier.
    /// </summary>
    int StateId { get; }

    /// <summary>
    /// Gets the state name.
    /// </summary>
    string StateName { get; }

    /// <summary>
    /// Called when entering this state.
    /// </summary>
    Task OnEnterAsync(object? context = null);

    /// <summary>
    /// Called when exiting this state.
    /// </summary>
    Task OnExitAsync(object? context = null);

    /// <summary>
    /// Updates the state (called each frame).
    /// </summary>
    Task OnUpdateAsync(float deltaTime);
}

/// <summary>
/// Represents an input event in the Finite State Machine.
/// </summary>
public interface IFsmInput
{
    /// <summary>
    /// Gets the input identifier.
    /// </summary>
    int InputId { get; }

    /// <summary>
    /// Gets the input name.
    /// </summary>
    string InputName { get; }

    /// <summary>
    /// Gets optional input data.
    /// </summary>
    object? Data { get; }
}

/// <summary>
/// Represents a transition in the Finite State Machine.
/// </summary>
public interface IFsmTransition
{
    /// <summary>
    /// Gets the source state ID.
    /// </summary>
    int FromStateId { get; }

    /// <summary>
    /// Gets the destination state ID.
    /// </summary>
    int ToStateId { get; }

    /// <summary>
    /// Gets the input ID that triggers this transition.
    /// </summary>
    int InputId { get; }

    /// <summary>
    /// Checks if this transition can be executed.
    /// </summary>
    Task<bool> CanTransitionAsync(object? context = null);
}
