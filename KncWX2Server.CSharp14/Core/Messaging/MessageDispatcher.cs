using System.Collections.Concurrent;

namespace KncWX2Server.Core.Messaging;

/// <summary>
/// Dispatches messages to appropriate handlers based on message type.
/// </summary>
public sealed class MessageDispatcher : IDisposable
{
    private readonly ConcurrentDictionary<int, IMessageHandler> _handlers;
    private readonly ReaderWriterLockSlim _lock;
    private bool _disposed;

    public MessageDispatcher()
    {
        _handlers = new();
        _lock = new();
    }

    /// <summary>
    /// Registers a message handler.
    /// </summary>
    public void RegisterHandler(IMessageHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers.AddOrUpdate(handler.MessageType, handler, (_, _) => handler);
    }

    /// <summary>
    /// Unregisters a message handler.
    /// </summary>
    public bool UnregisterHandler(int messageType)
    {
        return _handlers.TryRemove(messageType, out _);
    }

    /// <summary>
    /// Gets a handler for a specific message type.
    /// </summary>
    public IMessageHandler? GetHandler(int messageType)
    {
        _handlers.TryGetValue(messageType, out var handler);
        return handler;
    }

    /// <summary>
    /// Dispatches a message to the appropriate handler.
    /// </summary>
    public async Task<bool> DispatchAsync(int messageType, byte[] data, int length, object? sender = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        var handler = GetHandler(messageType);
        if (handler == null)
        {
            return false;
        }

        var context = new MessageContext(messageType, data, length, sender);
        return await handler.HandleAsync(sender ?? this, context);
    }

    /// <summary>
    /// Dispatches multiple messages.
    /// </summary>
    public async Task<int> DispatchMultipleAsync(
        IEnumerable<(int MessageType, byte[] Data, int Length)> messages,
        object? sender = null)
    {
        var tasks = messages.Select(m => DispatchAsync(m.MessageType, m.Data, m.Length, sender));
        var results = await Task.WhenAll(tasks);
        return results.Count(r => r);
    }

    /// <summary>
    /// Gets all registered handlers.
    /// </summary>
    public IReadOnlyDictionary<int, IMessageHandler> GetAllHandlers()
    {
        return _handlers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Clears all handlers.
    /// </summary>
    public void ClearAllHandlers()
    {
        _handlers.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _handlers.Clear();
        _lock?.Dispose();
        _disposed = true;
    }
}
