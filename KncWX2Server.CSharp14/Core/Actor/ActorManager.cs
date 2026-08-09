using System.Collections.Concurrent;
using KncWX2Server.Core.Data;

namespace KncWX2Server.Core.Actor;

/// <summary>
/// Manages all actors in the game world.
/// </summary>
public sealed class ActorManager : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, Actor> _actors;
    private readonly IActorFactory _actorFactory;
    private readonly ReaderWriterLockSlim _lock;
    private bool _disposed;
    private CancellationTokenSource? _updateCts;
    private Task? _updateTask;

    public ActorManager(IActorFactory actorFactory)
    {
        ArgumentNullException.ThrowIfNull(actorFactory);
        _actorFactory = actorFactory;
        _actors = new();
        _lock = new();
    }

    /// <summary>
    /// Creates and registers a new actor.
    /// </summary>
    public async Task<Actor> CreateActorAsync(GameSession gameSession)
    {
        ArgumentNullException.ThrowIfNull(gameSession);
        var actor = await _actorFactory.CreateActorAsync(gameSession);
        await actor.InitializeAsync();
        _actors.TryAdd(actor.ActorId, actor);
        return actor;
    }

    /// <summary>
    /// Removes an actor by ID.
    /// </summary>
    public async Task<bool> RemoveActorAsync(string actorId)
    {
        ArgumentException.ThrowIfNullOrEmpty(actorId);
        if (_actors.TryRemove(actorId, out var actor))
        {
            await actor.DisposeAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets an actor by ID.
    /// </summary>
    public Actor? GetActor(string actorId)
    {
        ArgumentException.ThrowIfNullOrEmpty(actorId);
        _actors.TryGetValue(actorId, out var actor);
        return actor;
    }

    /// <summary>
    /// Gets all active actors.
    /// </summary>
    public IReadOnlyCollection<Actor> GetAllActors()
    {
        return _actors.Values.ToList();
    }

    /// <summary>
    /// Gets the count of actors.
    /// </summary>
    public int GetActorCount() => _actors.Count;

    /// <summary>
    /// Starts the actor update loop.
    /// </summary>
    public void StartUpdateLoop(float updateInterval = 0.016f) // ~60 FPS
    {
        if (_updateTask != null)
        {
            return; // Already running
        }

        _updateCts = new();
        _updateTask = RunUpdateLoopAsync(updateInterval, _updateCts.Token);
    }

    /// <summary>
    /// Stops the actor update loop.
    /// </summary>
    public async Task StopUpdateLoopAsync()
    {
        if (_updateCts != null)
        {
            _updateCts.Cancel();
            if (_updateTask != null)
            {
                try
                {
                    await _updateTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when canceling
                }
            }
            _updateCts = null;
            _updateTask = null;
        }
    }

    private async Task RunUpdateLoopAsync(float updateInterval, CancellationToken cancellationToken)
    {
        var delayMs = (int)(updateInterval * 1000);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var actors = _actors.Values.ToList();
                var updateTasks = actors.Select(actor => actor.UpdateAsync(updateInterval));
                await Task.WhenAll(updateTasks);
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Log exception but continue
                Console.Error.WriteLine($"Error in actor update loop: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Broadcasts a message to all actors.
    /// </summary>
    public async Task BroadcastMessageAsync(byte[] messageData, string? excludeActorId = null)
    {
        ArgumentNullException.ThrowIfNull(messageData);
        var actors = _actors.Values
            .Where(a => excludeActorId == null || a.ActorId != excludeActorId)
            .ToList();

        var tasks = actors.Select(a => a.OnMessageReceivedAsync(messageData));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Clears all actors.
    /// </summary>
    public async Task ClearAllActorsAsync()
    {
        var actors = _actors.Values.ToList();
        foreach (var actor in actors)
        {
            await RemoveActorAsync(actor.ActorId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await StopUpdateLoopAsync();
        await ClearAllActorsAsync();
        _updateCts?.Dispose();
        _lock?.Dispose();
        _disposed = true;
    }
}
