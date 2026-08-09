using KncWX2Server.Core.Data;
using KncWX2Server.Core.FSM;
using KncWX2Server.Core.Network;

namespace KncWX2Server.Core.Actor;

/// <summary>
/// Base actor class (replaces KActor from C++).
/// Represents any entity in the game world with state machine support.
/// </summary>
public abstract class Actor : IAsyncDisposable
{
    protected readonly GameSession GameSession;
    protected readonly FiniteStateMachine<int, int> StateMachine;
    private bool _disposed;

    public string ActorId { get; protected set; }
    public long? UserId => GameSession.UserId;
    public string? CharacterName => GameSession.CharacterName;
    public int Level => GameSession.Level;

    protected Actor(GameSession gameSession)
    {
        ArgumentNullException.ThrowIfNull(gameSession);
        GameSession = gameSession;
        ActorId = Guid.NewGuid().ToString();
        StateMachine = new();
    }

    /// <summary>
    /// Initializes the actor (called once on creation).
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Updates the actor state (called each frame).
    /// </summary>
    public virtual async Task UpdateAsync(float deltaTime)
    {
        await StateMachine.UpdateAsync(deltaTime);
    }

    /// <summary>
    /// Called when the actor receives a message from the client.
    /// </summary>
    public virtual async Task OnMessageReceivedAsync(byte[] messageData)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Called when the actor's session is about to close.
    /// </summary>
    public virtual async Task OnSessionClosingAsync(string reason)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Sends a message to the client.
    /// </summary>
    protected async Task SendMessageAsync(byte[] messageData)
    {
        await GameSession.NetworkSession.SendMessageAsync(messageData);
    }

    /// <summary>
    /// Adds experience to the actor.
    /// </summary>
    public virtual void AddExperience(long amount)
    {
        GameSession.AddExperience(amount);
    }

    /// <summary>
    /// Changes the actor's level.
    /// </summary>
    public virtual async Task SetLevelAsync(int newLevel)
    {
        GameSession.Level = newLevel;
        await OnLevelChangedAsync(newLevel);
    }

    /// <summary>
    /// Called when the actor's level changes.
    /// </summary>
    protected virtual Task OnLevelChangedAsync(int newLevel)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the actor's game session.
    /// </summary>
    public GameSession GetGameSession() => GameSession;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await DisposeAsyncCore();
        _disposed = true;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (GameSession is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            GameSession?.Dispose();
        }
    }
}

/// <summary>
/// Factory for creating actor instances.
/// </summary>
public interface IActorFactory
{
    /// <summary>
    /// Creates an actor instance for the given session.
    /// </summary>
    Task<Actor> CreateActorAsync(GameSession gameSession);
}
