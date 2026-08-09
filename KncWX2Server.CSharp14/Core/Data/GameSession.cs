using System.Collections.Concurrent;
using KncWX2Server.Core.Network;

namespace KncWX2Server.Core.Data;

/// <summary>
/// Represents a game session with actor information (replaces KActor from C++).
/// Uses C# 14 features including primary constructors and records.
/// </summary>
public class GameSession : IDisposable
{
    private readonly ConcurrentDictionary<string, object> _attributes;
    private bool _disposed;

    public string SessionId { get; }
    public ISession NetworkSession { get; }
    public long? UserId { get; set; }
    public string? CharacterName { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; }
    public DateTime CreatedAt { get; }
    public DateTime LastUpdateAt { get; private set; }

    /// <summary>
    /// Creates a new game session.
    /// </summary>
    public GameSession(string sessionId, ISession networkSession)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        ArgumentNullException.ThrowIfNull(networkSession);

        SessionId = sessionId;
        NetworkSession = networkSession;
        _attributes = new();
        CreatedAt = DateTime.UtcNow;
        LastUpdateAt = DateTime.UtcNow;
        Level = 1;
        Experience = 0;
    }

    /// <summary>
    /// Sets a custom attribute on this session.
    /// </summary>
    public void SetAttribute<T>(string key, T value) where T : notnull
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _attributes[key] = value;
    }

    /// <summary>
    /// Gets a custom attribute from this session.
    /// </summary>
    public T? GetAttribute<T>(string key) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        if (_attributes.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return null;
    }

    /// <summary>
    /// Tries to get a custom attribute value.
    /// </summary>
    public bool TryGetAttribute<T>(string key, out T? value) where T : class
    {
        value = GetAttribute<T>(key);
        return value != null;
    }

    /// <summary>
    /// Removes a custom attribute.
    /// </summary>
    public bool RemoveAttribute(string key)
    {
        return _attributes.TryRemove(key, out _);
    }

    /// <summary>
    /// Updates the last activity time.
    /// </summary>
    public void UpdateActivity()
    {
        LastUpdateAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds experience to the session.
    /// </summary>
    public void AddExperience(long amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative((int)amount);
        Experience += amount;
    }

    /// <summary>
    /// Checks if the session is still active.
    /// </summary>
    public bool IsActive => NetworkSession.IsConnected && !_disposed;

    /// <summary>
    /// Gets all attributes.
    /// </summary>
    public IReadOnlyDictionary<string, object> GetAllAttributes()
    {
        return _attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _attributes.Clear();
        _disposed = true;
    }
}

/// <summary>
/// Manager for game sessions (replaces session container functionality).
/// </summary>
public sealed class GameSessionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions;
    private readonly ReaderWriterLockSlim _lock;
    private bool _disposed;

    public GameSessionManager()
    {
        _sessions = new();
        _lock = new();
    }

    /// <summary>
    /// Registers a new game session.
    /// </summary>
    public void RegisterSession(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _sessions.TryAdd(session.SessionId, session);
    }

    /// <summary>
    /// Unregisters and removes a game session.
    /// </summary>
    public bool UnregisterSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Dispose();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    public GameSession? GetSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    /// <summary>
    /// Gets a session by user ID.
    /// </summary>
    public GameSession? GetSessionByUserId(long userId)
    {
        return _sessions.Values.FirstOrDefault(s => s.UserId == userId);
    }

    /// <summary>
    /// Gets all active sessions.
    /// </summary>
    public IReadOnlyCollection<GameSession> GetAllActiveSessions()
    {
        return _sessions.Values.Where(s => s.IsActive).ToList();
    }

    /// <summary>
    /// Gets the count of active sessions.
    /// </summary>
    public int GetActiveSessionCount()
    {
        return _sessions.Count(kvp => kvp.Value.IsActive);
    }

    /// <summary>
    /// Closes all sessions.
    /// </summary>
    public async Task CloseAllSessionsAsync(string reason = "Server shutdown")
    {
        var tasks = _sessions.Values
            .Where(s => s.IsActive)
            .Select(s => s.NetworkSession.CloseAsync(reason));

        await Task.WhenAll(tasks);
        _sessions.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var session in _sessions.Values)
        {
            session.Dispose();
        }

        _sessions.Clear();
        _lock?.Dispose();
        _disposed = true;
    }
}
