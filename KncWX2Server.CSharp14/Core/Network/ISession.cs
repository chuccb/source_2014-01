namespace KncWX2Server.Core.Network;

/// <summary>
/// Represents a network session (replaces KSession from C++).
/// Handles connection management and message processing.
/// </summary>
public interface ISession : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the unique session identifier.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the remote address of this session.
    /// </summary>
    string RemoteAddress { get; }

    /// <summary>
    /// Gets or sets the user ID associated with this session.
    /// </summary>
    long? UserId { get; set; }

    /// <summary>
    /// Gets whether this session is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the creation time of this session.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the last activity time of this session.
    /// </summary>
    DateTime LastActivityAt { get; }

    /// <summary>
    /// Sends a message to the client.
    /// </summary>
    Task SendMessageAsync(byte[] message);

    /// <summary>
    /// Closes the session.
    /// </summary>
    Task CloseAsync(string reason = "Normal");

    /// <summary>
    /// Event raised when a message is received.
    /// </summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Event raised when the session is closed.
    /// </summary>
    event EventHandler<SessionClosedEventArgs>? Closed;
}

/// <summary>
/// Arguments for message received events.
/// </summary>
public sealed class MessageReceivedEventArgs(byte[] data, int length) : EventArgs
{
    public byte[] Data { get; } = data;
    public int Length { get; } = length;
}

/// <summary>
/// Arguments for session closed events.
/// </summary>
public sealed class SessionClosedEventArgs(string reason) : EventArgs
{
    public string Reason { get; } = reason;
    public DateTime ClosedAt { get; } = DateTime.UtcNow;
}
